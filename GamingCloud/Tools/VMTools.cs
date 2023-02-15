using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Resources;

namespace GamingCloud.Tools;

public class VMTools
{
    private ArmClient client;
    private ResourceGroupResource resourceGroup;
    
    private string resourceName;
    private string LOCATION;

    
    public VMTools(string username)
    {
        resourceName = username;

        client = new ArmClient(new DefaultAzureCredential());
        LOCATION = AzureLocation.FranceCentral;
    }

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
            var resourceGroupData = new ResourceGroupData(LOCATION);
        
            //Create Resource group
            await resourceGroups.CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName, resourceGroupData);

            //Return new resource group
            resourceGroup = await resourceGroups.GetAsync(resourceGroupName);
        }
        
        resourceGroup = await resourceGroups.GetAsync(resourceGroupName);
        
    }

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

    public async Task<string> GetPublicIpAddressAsync()
    {
        return CreatePublicIp().Data.IPAddress;
    }
    
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
                Location = LOCATION
            }).Value;
    }

    private VirtualNetworkResource CreateVirtualNet()
    {
        VirtualNetworkCollection vns = resourceGroup.GetVirtualNetworks();

        //Create vnetResource
        return vns.CreateOrUpdate(
            WaitUntil.Completed,
            $"vnet-{resourceName}-cloud-gaming",
            new VirtualNetworkData()
            {
                Location = LOCATION,
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
        NetworkInterfaceCollection nics = resourceGroup.GetNetworkInterfaces();

        //Create network interface
        return nics.CreateOrUpdate(
            WaitUntil.Completed,
            $"nic-{resourceName}-cloud-gaming",
            new NetworkInterfaceData()
            {
                Location = LOCATION,
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
            $"vm-{resourceName}-cloud-gaming",
            new VirtualMachineData(LOCATION)
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

    public async Task EnableAsync()
    {
        var vms = resourceGroup.GetVirtualMachines();

        foreach (var vm in vms)
        {
            await vm.PowerOnAsync(WaitUntil.Completed);
        }
    }

    public async Task DisableAsync()
    {
        var vms = resourceGroup.GetVirtualMachines();

        foreach (var vm in vms)
        {
            await vm.PowerOffAsync(WaitUntil.Completed);
        }
    }
}