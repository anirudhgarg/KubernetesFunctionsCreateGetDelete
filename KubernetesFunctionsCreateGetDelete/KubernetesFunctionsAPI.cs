using System;
using System.Collections.Generic;
using System.IO;
using k8s;

namespace KubernetesFunctions
{
    public class KubernetestFunctionsAPI
    {
        IKubernetes client;
        public KubernetestFunctionsAPI()
        {
            var config = KubernetesClientConfiguration.BuildDefaultConfig();           
            client = new Kubernetes(config);
        }

        public KubernetestFunctionsAPI(string filePath)
        {           
            FileInfo file = new FileInfo(filePath);
            var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(file);
            client = new Kubernetes(config);
        }

        public bool CreateFunctionsContainer(string containerName, out string status, string image = "mcr.microsoft.com/azure-functions/python")
        {            
            var deploymentList = client.ListDeploymentForAllNamespaces();
            foreach (var item in deploymentList.Items)
            {
                if (item.Metadata.Name == containerName)
                {
                    status = string.Format("Deployment {0} found! Skipping creation", item.Metadata.Name);
                    return false;
                }
            }


            try
            {
                /*Creating Deployment*/

                var containerport = new k8s.Models.V1ContainerPort
                {
                    ContainerPort = 8080
                };

                var containerports = new List<k8s.Models.V1ContainerPort>
                {
                    containerport
                };

                var container = new k8s.Models.V1Container
                {
                    Name = containerName,
                    Image = image,
                    Ports = containerports
                };

                var containers = new List<k8s.Models.V1Container>
                {
                    container
                };

                var podSpec = new k8s.Models.V1PodSpec
                {
                    Containers = containers
                };

                var template = new k8s.Models.V1PodTemplateSpec
                {
                    Metadata = new k8s.Models.V1ObjectMeta { Labels = new Dictionary<string, string>() { { "run", containerName } } },
                    Spec = podSpec
                };

                var spec = new k8s.Models.Appsv1beta1DeploymentSpec
                {
                    Selector = new k8s.Models.V1LabelSelector { MatchLabels = new Dictionary<string, string>() { { "run", containerName } } },
                    Template = template
                };

                var deployment = new k8s.Models.Appsv1beta1Deployment
                {
                    Metadata = new k8s.Models.V1ObjectMeta { Name = containerName },
                    Spec = spec
                };


                client.CreateNamespacedDeployment1(deployment, "default");
          

                /*Creating Service*/
                var serviceport = new k8s.Models.V1ServicePort
                {
                    Port = 8080
                };

                var serviceports = new List<k8s.Models.V1ServicePort>
                {
                    serviceport
                };

                var service = new k8s.Models.V1Service
                {
                    Spec = new k8s.Models.V1ServiceSpec
                    {
                        Type = "LoadBalancer",
                        Selector = new Dictionary<string, string>() { { "run", containerName } },
                        Ports = serviceports
                    },
                    Metadata = new k8s.Models.V1ObjectMeta
                    {
                        Name = containerName,
                        Labels = new Dictionary<string, string>() { { "run", containerName } }
                    }
                };

                client.CreateNamespacedService(service, "default");
            }
            catch (Exception ex)
            {
                status = ex.Message;
                return false;
            }

            status = string.Format("Created container {0}", containerName);
            return true;
        }

        public bool GetIPAdress(string containerName, out string IPAddress, out string status)
        {
            var services = client.ListServiceForAllNamespaces();
            foreach (var item in services.Items)
            {
                if(item.Metadata.Name == containerName)
                {
                    if (item.Status.LoadBalancer.Ingress != null)
                    {
                        IPAddress = item.Status.LoadBalancer.Ingress[0].Ip;
                        status = "IP Address found";
                        return true;
                    }
                    else
                    {
                        IPAddress = string.Empty;
                        status = "IP Address not yet assigned";
                        return false;
                    }
                }
            }
            IPAddress = string.Empty;
            status = "Container not found";
            return false;
        }

        public bool DeleteContainer(string containerName, out string status)
        {
            try
            {
                var services = client.ListServiceForAllNamespaces();
                foreach (var item in services.Items)
                {
                    if (item.Metadata.Name == containerName)
                    {
                        client.DeleteNamespacedService(containerName, "default");
                    }
                }

                var deploymentList = client.ListDeploymentForAllNamespaces();
                foreach (var item in deploymentList.Items)
                {
                    if (item.Metadata.Name == containerName)
                    {
                        client.DeleteNamespacedDeployment1(containerName, "default");
                    }
                }
            }
            catch(Exception ex)
            {
                status = ex.Message;
                return false;
            }

            status = string.Format("The container {0} was deleted successfully.", containerName);
            return true;               
        }
    }
}
