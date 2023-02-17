using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Resources;
using ImageReference = Azure.ResourceManager.Compute.Models.ImageReference;
using LinuxConfiguration = Azure.ResourceManager.Compute.Models.LinuxConfiguration;

namespace GamingCloud.Tools;

public class AzureTools
{
    private ArmClient client;
    private ResourceGroupResource resourceGroup;
    
    private string resourceName;

    public AzureTools(string username)
    {
        resourceName = username;

        var token = new ClientSecretCredential("b7b023b8-7c32-4c02-92a6-c8cdaa1d189c", "8850c7b5-603e-4238-b19e-11833f42c92a", "akU8Q~CNURbvnquYagP0G~qYUkOnwn-E2aZGkclD");

        client = new ArmClient(token);

    }

    /// <summary>
    /// Get resource group or create if it doesn't exist
    /// </summary>
    public async Task SetResourceGroupAsync()
    {
        // Now we get a ResourceGroupResource collection for that subscription
        var subscription = await client.GetDefaultSubscriptionAsync();
        var resourceGroups = subscription.GetResourceGroups();

        // With the collection, we can create a new resource group with an specific name
        var resourceGroupName = $"rg-{resourceName}-cloud-gaming";
        
        //Check if resource exist or not
        bool exists = await resourceGroups.ExistsAsync(resourceGroupName);
        
        //If exists than return it
        if (!exists)
        {
            //Set location
            var resourceGroupData = new ResourceGroupData(AzureLocation.FranceCentral);
        
            //Create Resource group
            await resourceGroups.CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName, resourceGroupData);
        }
        
        resourceGroup = await resourceGroups.GetAsync(resourceGroupName);
        
    }

    /// <summary>
    /// Remove resource group with all elements inside
    /// </summary>
    public async Task RemoveResourceGroupAsync()
    {
        // Now we get a ResourceGroupResource collection for that subscription
        var subscription = await client.GetDefaultSubscriptionAsync();
        var resourceGroups = subscription.GetResourceGroups();

        // With the collection, we can create a new resource group with an specific name
        var resourceGroupName = $"rg-{resourceName}-cloud-gaming";
        
        //Check if resource exist or not
        bool exists = await resourceGroups.ExistsAsync(resourceGroupName);
        
        //If it's doesn't exists than return it
        if (!exists) return;
        
        var resourceGroup = await resourceGroups.GetAsync(resourceGroupName);

        await resourceGroup.Value.DeleteAsync(WaitUntil.Completed, "Microsoft.Compute/virtualMachines");

    }

    /// <summary>
    /// Get ip adresse after creation of virtual machine
    /// </summary>
    /// <returns></returns>
    public async Task<string> GetPublicIpAddressAsync()
    {
        return CreatePublicIp().Data.IPAddress;
    }
    
    /// <summary>
    /// Create ip adresse
    /// </summary>
    /// <returns></returns>
    private PublicIPAddressResource CreatePublicIp()
    {
        PublicIPAddressCollection publicIps = resourceGroup.GetPublicIPAddresses();

        //Create public ip
        return publicIps.CreateOrUpdate(
            WaitUntil.Completed,
            $"ip-{resourceName}-cloud-gaming",
            new PublicIPAddressData()
            {
                PublicIPAddressVersion = NetworkIPVersion.IPv4,
                PublicIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                Location = AzureLocation.FranceCentral
            }).Value;
    }

    /// <summary>
    /// Create vnet
    /// </summary>
    /// <returns></returns>
    private VirtualNetworkResource CreateVirtualNet()
    {
        VirtualNetworkCollection vns = resourceGroup.GetVirtualNetworks();

        //Create vnetResource
        return vns.CreateOrUpdate(
            WaitUntil.Completed,
            $"vnet-{resourceName}-cloud-gaming",
            new VirtualNetworkData()
            {
                Location = AzureLocation.FranceCentral,
                Subnets =
                {
                    new SubnetData()
                    {
                        Name = "testSubNet",
                        AddressPrefix = "10.0.0.0/24"
                    }
                },
                AddressPrefixes =
                {
                    "10.0.0.0/16"
                },
        }).Value;

    }

    /// <summary>
    /// Create network interface
    /// </summary>
    /// <param name="vnet"></param>
    /// <param name="ipResource"></param>
    /// <returns></returns>
    private NetworkInterfaceResource CreateNetworkInterface(VirtualNetworkResource vnet, PublicIPAddressResource ipResource)
    {
        NetworkInterfaceCollection nics = resourceGroup.GetNetworkInterfaces();

        //Create network interface
        return nics.CreateOrUpdate(
            WaitUntil.Completed,
            $"nic-{resourceName}-cloud-gaming",
            new NetworkInterfaceData()
            {
                Location = AzureLocation.FranceCentral,
                IPConfigurations =
                {
                    new NetworkInterfaceIPConfigurationData()
                    {
                        Name = "Primary",
                        Primary = true,
                        Subnet = new SubnetData() { Id = vnet?.Data.Subnets.First().Id },
                        PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                        PublicIPAddress = new PublicIPAddressData() { Id = ipResource?.Data.Id }
                    }
                }
            }).Value;
    }

    /// <summary>
    /// Get Custom image generate before, stored in resource group img-linux
    /// </summary>
    /// <returns></returns>
    private async Task<DiskImageResource> GetCustomImageAsync()
    {
        var subscription = await client.GetDefaultSubscriptionAsync();
        var resourceGroups = subscription.GetResourceGroups();
        
        var resourceGroup = await resourceGroups.GetAsync("img-linux");
        
        return resourceGroup.Value.GetDiskImages().Get("img-linux-snake").Value;
        
    }

    /// <summary>
    /// Create virtual machine with ip, vnet and interfaceNetwork
    /// </summary>
    /// <param name="adminUsername"></param>
    /// <param name="adminPassword"></param>
    /// <returns></returns>
    public async Task<VirtualMachineResource> CreateVirtualMachine(string adminUsername, string adminPassword)
    {

        var ip = CreatePublicIp();
        var vnet = CreateVirtualNet();
        var interfaceNetwork = CreateNetworkInterface(vnet, ip);
        
        //Get collection virtual machines
        VirtualMachineCollection vms = resourceGroup.GetVirtualMachines();
        
        //Get Custom image
        DiskImageResource customImg = await GetCustomImageAsync();
        
        //Virtual machine
        var vm = vms.CreateOrUpdate(
            WaitUntil.Completed,
            $"vm-{resourceName}-cloud-gaming",
            new VirtualMachineData(AzureLocation.FranceCentral)
            {
                HardwareProfile = new VirtualMachineHardwareProfile()
                {
                    VmSize = VirtualMachineSizeType.StandardB1S
                },
                OSProfile = new VirtualMachineOSProfile()
                {
                    ComputerName = $"machine-{resourceName}",
                    AdminUsername = adminUsername,
                    AdminPassword = adminPassword,
                    LinuxConfiguration = new LinuxConfiguration() 
                    { 
                        DisablePasswordAuthentication = false, 
                        ProvisionVmAgent = true 
                    }
                },
                StorageProfile = new VirtualMachineStorageProfile()
                {
                    OSDisk = new VirtualMachineOSDisk(DiskCreateOptionType.FromImage)
                    {
                        DeleteOption = DiskDeleteOptionType.Delete,
                    },
                    ImageReference = new ImageReference()
                    {
                        Id = customImg.Id
                    }
                },
                NetworkProfile = new VirtualMachineNetworkProfile()
                {
                    NetworkInterfaces =
                    {
                        new VirtualMachineNetworkInterfaceReference()
                        {
                            Id = interfaceNetwork.Id
                        }
                    }
                },
            }).Value;
        
        return vm;

    }

    /// <summary>
    /// Start virtual machine
    /// </summary>
    public async Task EnableAsync()
    {
        var vms = resourceGroup.GetVirtualMachines();
        
        var vm = await vms.GetAsync($"vm-{resourceName}-cloud-gaming");

        await vm.Value.PowerOnAsync(WaitUntil.Completed);
        
    }

    /// <summary>
    /// Stop virtual machine
    /// </summary>
    public async Task DisableAsync()
    {
        var vms = resourceGroup.GetVirtualMachines();

        var vm = await vms.GetAsync($"vm-{resourceName}-cloud-gaming");

        await vm.Value.PowerOffAsync(WaitUntil.Completed);
    }
}