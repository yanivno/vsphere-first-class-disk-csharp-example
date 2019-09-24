using AppUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Vim25Api;
using CommandLine;

namespace fcdSampleCode
{
    class Program
    {
        private static string CONN_TYPE = "ServiceInstance";

        private static string VC_URL = "https://yaniv-vcsa-01a.pso-il.local/sdk";
        private static string VC_USERNAME = "administrator@vsphere.local";
        private static string VC_PASSWORD = "VMware1!";
        private static string VC_DATASTORE_NAME = "vsanDatastore";

        //create disk properties
        private static string VC_VDISK_NAME = "yaniv_test";
        private static long VC_VDISK_SIZE = 1024;
        private static string VC_PROVISIONING_TYPE = "thin";

        //vdisk link clone
        private static string VC_VDISK_SNAPSHOT_PREFIX = "base-snapshot-";

        //general
        private static string VC_VM_NAME = "centos";

        private string username;
        private string password;
        private string url;
        private SvcConnection svcConn;
        private AppUtil.AppUtil appUtil;
        private ServiceUtil svcUtil;

        public Program(string vcUrl, string vcUsername, string vcPassword)
        {
            this.username = vcUsername;
            this.password = vcPassword;
            this.url = vcUrl;

            svcConn = new SvcConnection(CONN_TYPE);
            svcConn.ignoreCert = true;
            svcConn.Connect(vcUrl, vcUsername, vcPassword);

            svcUtil = new ServiceUtil();
            appUtil = new AppUtil.AppUtil(CONN_TYPE);
            appUtil._connection = svcConn;

            svcUtil.Init(appUtil);
        }

        public VStorageObject CreateNewVDisk(ManagedObjectReference dsMoref, string name, bool keepAfterDelete, string provisioningType, long sizeInMB )
        {
            ManagedObjectReference vStorageMgr = svcConn.ServiceContent.vStorageObjectManager;
            VslmCreateSpec spec = new VslmCreateSpec();
            spec.name = name;
            spec.keepAfterDeleteVm = keepAfterDelete;
            var backing = new VslmCreateSpecDiskFileBackingSpec();
            backing.datastore = dsMoref;
            backing.provisioningType = provisioningType;
            spec.backingSpec = backing;
            spec.capacityInMB = sizeInMB;
            var task = svcConn._service.CreateDisk_Task(vStorageMgr, spec);

            var newDiskTaskResult = svcUtil.WaitForValues(task,
                new string[] { "info.state", "info.result" },
                new string[] { "state" }, // info has a property - state for state of the task
                new object[][] { new object[] { TaskInfoState.success, TaskInfoState.error } });

            if (newDiskTaskResult[0].Equals(TaskInfoState.success))
            {
                ObjectContent[] objTaskInfo = svcUtil.GetObjectProperties(svcConn._sic.propertyCollector, task, new String[] { "info" });
                TaskInfo tInfo = (TaskInfo)objTaskInfo[0].propSet[0].val; ;
                VStorageObject newDisk = (VStorageObject)tInfo.result;
                return newDisk;
            } else
            {
                throw new Exception("task failed on vCenter Server");
            }
        }

        public VStorageObject GetVDiskByName(ManagedObjectReference dsMoref, string vdiskName )
        {
            ManagedObjectReference vStorageMgr = svcConn.ServiceContent.vStorageObjectManager;
            ListVStorageObjectRequest req = new ListVStorageObjectRequest(vStorageMgr, dsMoref);
            ListVStorageObjectResponse response = svcConn._service.ListVStorageObject(req);
            VStorageObject result = null;
            foreach (ID id in response.returnval)
            {
                VStorageObject disk = svcConn._service.RetrieveVStorageObject(vStorageMgr, id, dsMoref);
                if (disk.config.name == vdiskName)
                {
                    if (result == null) { result = disk; }
                    Console.WriteLine("found disk = " + disk.config.id.id);
                }
            }

            if (result == null)
            {
                throw new Exception("could not find vDisk with name=" + vdiskName);
            }

            return result;
        }

        public ID CreateVDiskSnapshot(ManagedObjectReference dsMoref, ID vDiskId, string snapshotDescription)
        {
            ManagedObjectReference vStorageMgr = svcConn.ServiceContent.vStorageObjectManager;
            ManagedObjectReference task = svcConn._service.VStorageObjectCreateSnapshot_Task(vStorageMgr, vDiskId, dsMoref, snapshotDescription);
            var newDiskTaskResult = svcUtil.WaitForValues(task,
                new string[] { "info.state", "info.result" },
                new string[] { "state" }, 
                new object[][] { new object[] { TaskInfoState.success, TaskInfoState.error } });

            ObjectContent[] objTaskInfo = svcUtil.GetObjectProperties(svcConn._sic.propertyCollector, task, new String[] { "info" });
            TaskInfo tInfo = (TaskInfo)objTaskInfo[0].propSet[0].val;

            if (newDiskTaskResult[0].Equals(TaskInfoState.error))
                throw new Exception(tInfo.error.localizedMessage);
            else
            {
                ID newSnapshot = (ID)tInfo.result;
                return newSnapshot;
            }
        }

        public VStorageObject CreateDiskFromSnapshot(ManagedObjectReference dsMoref, ID vdiskId, ID snapshotId, string newDiskName)
        {
            VirtualMachineProfileSpec[] profile = null;
            string path = null;
            CryptoSpec crypto = null;
            ManagedObjectReference vStorageManager = svcConn._sic.vStorageObjectManager;

            CreateDiskFromSnapshot_TaskRequest req = new CreateDiskFromSnapshot_TaskRequest(vStorageManager, vdiskId, dsMoref, snapshotId, newDiskName, profile, crypto, path);
            var task = svcConn._service.CreateDiskFromSnapshot_Task(req).returnval;
            var newDiskTaskResult = svcUtil.WaitForValues(task,
                new string[] { "info.state", "info.result" },
                new string[] { "state" },
                new object[][] { new object[] { TaskInfoState.success, TaskInfoState.error } });


            ObjectContent[] objTaskInfo = svcUtil.GetObjectProperties(svcConn._sic.propertyCollector, task, new String[] { "info" });
            TaskInfo tInfo = (TaskInfo)objTaskInfo[0].propSet[0].val;

            if (newDiskTaskResult[0].Equals(TaskInfoState.error))
                throw new Exception(tInfo.error.localizedMessage);
            else {
                VStorageObject newVdisk = (VStorageObject)tInfo.result;
                return newVdisk;
            }
        }

        public Tuple<int,int> GetControllerPlacement(ManagedObjectReference vm)
        {
            var obj = svcUtil.GetObjectProperties(svcConn._sic.propertyCollector, vm, new string[] { "config.hardware.device" });
            VirtualDevice[] devices = (VirtualDevice[])obj[0].propSet[0].val;

            Dictionary<int, VirtualDevice> devMap = new Dictionary<int, VirtualDevice>();
            foreach (VirtualDevice device in devices) {
                devMap.Add(device.key, device);
            }

            foreach (VirtualDevice device in devices) {
                if (device is VirtualSCSIController) {
                    int[] slots = Enumerable.Repeat(0, 16).ToArray(); //init array with 0, has 16 places
                    VirtualSCSIController c = (VirtualSCSIController)device;
                    foreach (var deviceKey in c.device) {
                        var unitNumber = devMap[deviceKey].unitNumber;
                        slots[unitNumber] = 1;
                    }

                    for (int i = 0; i < slots.Count(); i++) {
                        if (slots[i] != 1)
                            return new Tuple<int, int>(c.key, i);
                    }
                }
            }

            throw new Exception("Could not find a SCSI Controller in VM");
        }

        public void AttachDiskToVm(ManagedObjectReference vm, ID diskId, ManagedObjectReference ds, int controllerKey, int unitNumber)
        {
            ManagedObjectReference task = svcConn._service.AttachDisk_Task(vm, diskId, ds, controllerKey, unitNumber);
            var newDiskTaskResult = svcUtil.WaitForValues(task,
                new string[] { "info.state", "info.result" },
                new string[] { "state" },
                new object[][] { new object[] { TaskInfoState.success, TaskInfoState.error } });

            ObjectContent[] objTaskInfo = svcUtil.GetObjectProperties(svcConn._sic.propertyCollector, task, new String[] { "info" });
            TaskInfo tInfo = (TaskInfo)objTaskInfo[0].propSet[0].val;
               
            if (newDiskTaskResult[0].Equals(TaskInfoState.error))
                throw new Exception(tInfo.error.localizedMessage);

        }

        static void Main(string[] args)
        {

            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            CommandLine.Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
            {
                Program p = new Program(o.Server, o.Username, o.Password);

                ManagedObjectReference dsMoref = p.svcUtil.getEntityByName("Datastore", o.Datastore);
                ManagedObjectReference vm = p.svcUtil.getEntityByName("VirtualMachine", o.VMName);

                var diskPlacement = p.GetControllerPlacement(vm);

                VStorageObject vdisk = p.GetVDiskByName(dsMoref, o.FcdName);
                Console.WriteLine("Using vDisk id=" + vdisk.config.id.id);
                Console.WriteLine("Using vDisk Name=" + vdisk.config.name);

                string snapshotName = o.SnapshotNamePrefix + Guid.NewGuid();
                Console.WriteLine("going to create snapshot with name=" + snapshotName);

                ID snapshotId = p.CreateVDiskSnapshot(dsMoref, vdisk.config.id, snapshotName);
                Console.WriteLine("created snapshot with id=" + snapshotId.id);

                string newDiskName = o.FcdName + "_" + snapshotId.id;
                Console.WriteLine("Creating a new vDisk with name=" + newDiskName);

                VStorageObject newDisk = p.CreateDiskFromSnapshot(dsMoref, vdisk.config.id, snapshotId, newDiskName);
                Console.WriteLine("created a new  vDisk with id=" + newDisk.config.id.id);

                string vmdkPath = ((BaseConfigInfoDiskFileBackingInfo)newDisk.config.backing).filePath;
                Console.WriteLine("created a new vDisk in path=" + vmdkPath);

                Console.WriteLine("Attaching new vDisk to VM=" + vm.Value);
                p.AttachDiskToVm(vm, newDisk.config.id, dsMoref, diskPlacement.Item1, diskPlacement.Item2);
                Console.WriteLine("Attached disk to vm in SCSI Controller=" + diskPlacement.Item1);
                Console.WriteLine("Attached disk to vm in SCSI UnitNumber=" + diskPlacement.Item2);

            });

            Console.WriteLine("Press Enter to continue...");
            Console.Read();
        }
    }
}
