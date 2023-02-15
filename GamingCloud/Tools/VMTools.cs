using Azure;
using Azure.Core;
using Microsoft.Azure.Management.Compute.Models;

namespace GamingCloud.Tools;

using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Resources;

public class VMTools
{

    private ArmClient client;
    private ResourceGroupResource resourceGroup;
    private string resourceName;
    
    public VMTools(string resourceName)
    {
        client = new ArmClient(new DefaultAzureCredential());
        this.resourceName = resourceName;
    }
    
    public async Task<ResourceGroupResource> SetInitResouceGroup()
    {
        resourceGroup = await GetResourceGroup();
        return resourceGroup;
    }

    private async Task<ResourceGroupResource> GetResourceGroup()
    {

        // Now we get a ResourceGroupResource collection for that subscription
        var subscription = await client.GetDefaultSubscriptionAsync();
        var resourceGroups = subscription.GetResourceGroups();

        // With the collection, we can create a new resource group with an specific name
        var resourceGroupName = $"rg-21-{resourceName}-gaming";
        
        //Check if resource exist or not
        bool exists = await resourceGroups.ExistsAsync(resourceGroupName);
        
        //If exists than return it
        if (exists)
            return await resourceGroups.GetAsync(resourceGroupName);

        //Set location
        var location = AzureLocation.FranceCentral;
        var resourceGroupData = new ResourceGroupData(location);
        
        //Create Resource group
        await resourceGroups.CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName, resourceGroupData);

        //Return new resource group
        return await resourceGroups.GetAsync(resourceGroupName);
    }

    public async Task RemoveResouceGroup()
    {
        // Now we get a ResourceGroupResource collection for that subscription
        var subscription = await client.GetDefaultSubscriptionAsync();
        var resourceGroups = subscription.GetResourceGroups();

        // With the collection, we can create a new resource group with an specific name
        var resourceGroupName = $"rg-21-{resourceName}-gaming";
        
        //Check if resource exist or not
        bool exists = await resourceGroups.ExistsAsync(resourceGroupName);
        
        //If it's doesn't exists than return it
        if (!exists) return;
        
        var resourceGroup = await resourceGroups.GetAsync(resourceGroupName);

        await resourceGroup.Value.DeleteAsync(WaitUntil.Completed, "Microsoft.Compute/virtualMachines");

    }

    public async Task<string> GetPublicIpAdress()
    {
        var resourceGroup = await GetResourceGroup();
        string IP_NAME = $"ip-{resourceName}-gaming";


        var a = await resourceGroup.GetPublicIPAddressAsync(IP_NAME);

        return a.Value.Data.IPAddress;
    }
    
    private PublicIPAddressResource CreatePublicIp()
    {
        string IP_NAME = $"ip-{resourceName}-gaming";
        PublicIPAddressCollection publicIps = resourceGroup.GetPublicIPAddresses();

        //Create public ip
        return publicIps.CreateOrUpdate(
            WaitUntil.Completed,
            IP_NAME,
            new PublicIPAddressData()
            {
                PublicIPAddressVersion = NetworkIPVersion.IPv4,
                PublicIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                Location = AzureLocation.FranceCentral
            }).Value;
    }

    private VirtualNetworkResource CreateVirtualNet()
    {
        string VNET_NAME = $"vnet-{resourceName}-gaming";
        VirtualNetworkCollection vns = resourceGroup.GetVirtualNetworks();

        //Create vnetResource
        return vns.CreateOrUpdate(
            WaitUntil.Completed,
            VNET_NAME,
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

    private NetworkInterfaceResource CreateNetworkInterface(VirtualNetworkResource vnet, PublicIPAddressResource ipResource)
    {
        string NETWORK_INTERFACE_NAME = $"network-interface-{resourceName}-gaming";
        NetworkInterfaceCollection nics = resourceGroup.GetNetworkInterfaces();

        //Create network interface
        return nics.CreateOrUpdate(
            WaitUntil.Completed,
            NETWORK_INTERFACE_NAME,
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

    public VirtualMachineResource CreateVirtualMachine(string adminUsername, string adminPassword)
    {

        var ip = CreatePublicIp();
        var vnet = CreateVirtualNet();
        var interfaceNetwork = CreateNetworkInterface(vnet, ip);
        
        VirtualMachineCollection vms = resourceGroup.GetVirtualMachines();
        
        //Virtual machine
        return vms.CreateOrUpdate(
            WaitUntil.Completed,
            $"machine-{resourceName}",
            new VirtualMachineData(AzureLocation.FranceCentral)
            {
                HardwareProfile = new VirtualMachineHardwareProfile()
                {
                    VmSize = VirtualMachineSizeType.StandardB1S
                },
                OSProfile = new VirtualMachineOSProfile()
                {
                    ComputerName = $"VM-{resourceName}",
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
                    OSDisk = new VirtualMachineOSDisk(DiskCreateOptionType.FromImage),
                    ImageReference = new ImageReference()
                    {
                        Offer = "UbuntuServer",
                        Publisher = "Canonical",
                        Sku = "18.04-LTS",
                        Version = "latest"
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
        
    }

    public async Task Enable()
    {
        var resourceGroup = await GetResourceGroup();

        var vms = resourceGroup.GetVirtualMachines();

        foreach (var vm in vms)
        {
            await vm.PowerOnAsync(WaitUntil.Completed);
        }
    }

    public async Task Disable()
    {
        var resourceGroup = await GetResourceGroup();

        var vms = resourceGroup.GetVirtualMachines();

        foreach (var vm in vms)
        {
            await vm.PowerOffAsync(WaitUntil.Completed);
        }
    }
}