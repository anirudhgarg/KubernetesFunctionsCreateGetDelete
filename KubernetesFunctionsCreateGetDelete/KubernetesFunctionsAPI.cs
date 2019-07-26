using k8s;
using System;
using System.Collections.Generic;
using System.IO;

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

        public Dictionary<string,string> GetAllActiveFunctionContainers()
        {
            var containerIPAdresses = new Dictionary<string, string>();
            var services = client.ListServiceForAllNamespaces();
            foreach (var item in services.Items)
            {
                if (item.Metadata.Labels.ContainsKey("functions"))
                {
                    string IPAddress = string.Empty;
                    if (item.Status.LoadBalancer.Ingress != null)
                    {
                        IPAddress = item.Status.LoadBalancer.Ingress[0].Ip;
                      
                    }
                    containerIPAdresses.Add(item.Metadata.Name, IPAddress);
                }
            }
            return containerIPAdresses;
        }

        public bool CreateFunctionsContainer(string containerName, out string status, string image = "mcr.microsoft.com/azure-functions/mesh:2.0.12490", int numberOfContainers=1)
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
                    ContainerPort = 80
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
                    Metadata = new k8s.Models.V1ObjectMeta { Labels = new Dictionary<string, string>() { { "functions", containerName } } },
                    Spec = podSpec
                };

                var spec = new k8s.Models.Appsv1beta1DeploymentSpec
                {
                    Replicas = numberOfContainers,
                    Selector = new k8s.Models.V1LabelSelector { MatchLabels = new Dictionary<string, string>() { { "functions", containerName } } },
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
                    Port = 80
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
                        Selector = new Dictionary<string, string>() { { "functions", containerName } },
                        Ports = serviceports
                    },
                    Metadata = new k8s.Models.V1ObjectMeta
                    {
                        Name = containerName,
                        Labels = new Dictionary<string, string>() { { "functions", containerName } }
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

                var replicaSetList = client.ListReplicaSetForAllNamespaces();
                foreach (var item in replicaSetList.Items)
                {
                    if (item.Metadata.Name.StartsWith(containerName + "-"))
                    {
                        client.DeleteNamespacedReplicaSet(item.Metadata.Name, "default");
                    }
                }

                var podList = client.ListPodForAllNamespaces();
                foreach (var item in podList.Items)
                {
                    if (item.Metadata.Name.StartsWith(containerName + "-"))
                    {
                        client.DeleteNamespacedPod(item.Metadata.Name, "default");
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
