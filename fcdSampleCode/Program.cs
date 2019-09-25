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

        public Tuple<TaskInfoState, TaskInfo> WaitForTask(ManagedObjectReference taskMoref)
        {
            var taskResult = svcUtil.WaitForValues(taskMoref,
                new string[] { "info.state", "info.result" },
                new string[] { "state" },
                new object[][] { new object[] { TaskInfoState.success, TaskInfoState.error } });

            ObjectContent[] objTaskInfo = svcUtil.GetObjectProperties(svcConn._sic.propertyCollector, taskMoref, new String[] { "info" });
            TaskInfo tInfo = (TaskInfo)objTaskInfo[0].propSet[0].val;
            TaskInfoState taskState = (TaskInfoState)taskResult[0];
            return new Tuple<TaskInfoState,TaskInfo>(taskState,tInfo);
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

            var taskResult = WaitForTask(task);
            if (taskResult.Item1.Equals(TaskInfoState.success)) {
                VStorageObject newDisk = (VStorageObject)taskResult.Item2.result;
                return newDisk;
            } else {
                throw new Exception(taskResult.Item2.error.localizedMessage);
            }
        }

    
        public void ReconcileDatastoreInventory(ManagedObjectReference dsMoref)
        {
            ManagedObjectReference vStorageMgr = svcConn.ServiceContent.vStorageObjectManager;
            ManagedObjectReference task = svcConn._service.ReconcileDatastoreInventory_Task(vStorageMgr, dsMoref);
            var taskResult = WaitForTask(task);
            if (taskResult.Item1.Equals(TaskInfoState.error)) {
                throw new Exception(taskResult.Item2.error.localizedMessage);
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

            var taskResult = WaitForTask(task);
            if (taskResult.Item1.Equals(TaskInfoState.error))
                throw new Exception(taskResult.Item2.error.localizedMessage);
            else {
                ID newSnapshot = (ID)taskResult.Item2.result;
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
            var taskResult = WaitForTask(task);

            if (taskResult.Item1.Equals(TaskInfoState.error))
                throw new Exception(taskResult.Item2.error.localizedMessage);
            else {
                VStorageObject newVdisk = (VStorageObject)taskResult.Item2.result;
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
                    slots[7] = 1; //SCSI reserved number.
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

            throw new Exception("Could not find a free SCSI Controller slot in VM");
        }

        public void AttachDiskToVm(ManagedObjectReference vm, ID diskId, ManagedObjectReference ds, int controllerKey, int unitNumber)
        {
            ManagedObjectReference task = svcConn._service.AttachDisk_Task(vm, diskId, ds, controllerKey, unitNumber);
            var taskResult = WaitForTask(task);

            if (taskResult.Item1.Equals(TaskInfoState.error))
                throw new Exception(taskResult.Item2.error.localizedMessage);
        }

        static void Main(string[] args)
        {

            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            CommandLine.Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
            {
                Program p = new Program(o.Server, o.Username, o.Password);

                Console.WriteLine("Finding Datastore with name=" + o.Datastore);
                ManagedObjectReference dsMoref = p.svcUtil.getEntityByName("Datastore", o.Datastore);
                Console.WriteLine("Found Datastore with Moref=" + dsMoref.Value);

                Console.WriteLine("Finding VM with name=" + o.VMName);
                ManagedObjectReference vm = p.svcUtil.getEntityByName("VirtualMachine", o.VMName);
                Console.WriteLine("Found VM with Moref=" + vm.Value);

                Console.WriteLine("Finding a SCSI Placement for new disk");
                var diskPlacement = p.GetControllerPlacement(vm);
                Console.WriteLine("SCSI Controller=" + diskPlacement.Item1);
                Console.WriteLine("SCSI UnitNumber=" + diskPlacement.Item2);

                Console.WriteLine("Reconcile the datastore inventory info of virtual storage objects...");
                p.ReconcileDatastoreInventory(dsMoref); //in case files were deleted without vStorageManager is aware.

                Console.WriteLine("Finding VDisk with name=" + o.FcdName);
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
                Console.WriteLine("Created a new  vDisk with id=" + newDisk.config.id.id);

                string vmdkPath = ((BaseConfigInfoDiskFileBackingInfo)newDisk.config.backing).filePath;
                Console.WriteLine("Created a new vDisk in path=" + vmdkPath);

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
