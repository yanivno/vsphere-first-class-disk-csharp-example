# vSphere First Class Disk in C# Sample Code

Sample code for working with First Class Disks in vSphere written in C#.
No Warranty or Support is supplied with this sample.


## About this sample
This sample performs the following:
* Creates a Snapshot of Existing First Class Disk
* Creates a new First Class Disk from the Snapshot (i.e. Delta Disk)
* Mounts the new First Class Disk on an existing Virtual Machine.

TL;DR - [View Code](https://github.com/yanivno/vsphere-first-class-disk-csharp-example/blob/master/fcdSampleCode/Program.cs)

## Installation

No Installation is required. Just run the binary executable.

[Download Latest Release](https://github.com/yanivno/vsphere-first-class-disk-csharp-example/releases/latest)

## Dependencies 
* vSphere 6.7 Update 3
* vSphere 6.7 Update 3 DotNet SDK - [Download](https://my.vmware.com/group/vmware/get-download?downloadGroup=VS-MGMT-SDK67U3)
* Visual Studio 2015 Community Edition (Or Later)

## Running
```shell
fcdSampleCode 1.0.2.0
Copyright Yaniv Norman c  2019

  -s, --server       Required. vCenter SDK Url in https://<vcenter>/sdk

  -u, --username     Required. Username for vCenter Connection

  -p, --password     Required. password for vCenter Connection

  -d, --datastore    Required. Datastore name

  -f, --fcd          Required. Existing First Class Disk Name

  -v, --vm           Required. Target VM Name

  -n, --snapshot     Required. Snapshot Name Prefix, i.e. snap-

  --help             Display this help screen.

  --version          Display version information.

Press Enter to continue...
```

Sample Command
```shell
fcdSampleCode.exe -s "https://yaniv-vcsa-01a.pso-il.local/sdk" -u "administrator@vsphere.local" -p "VMware1!" -d "vsanDatastore" -f "yaniv_test" -v "centos" -n "snap-prefix-"
```

Example Output
```shell
[ 9/25/2019 1:54:05 PM ] Begin Log.
Finding Datastore with name=vsanDatastore
Found Datastore with Moref=datastore-16
Finding VM with name=centos
Found VM with Moref=vm-27
Finding a SCSI Placement for new disk
SCSI Controller=1000
SCSI UnitNumber=10
Reconcile the datastore inventory info of virtual storage objects...
Finding VDisk with name=yaniv_test
found disk = 167d6f3b-a74d-4e47-b2fd-30734a2f4180
found disk = 2ed4aa94-1bdd-4b02-93b9-4aee70003dd9
found disk = 3f5ff6d7-a03a-498c-a123-99c942d94cc9
found disk = 56ebb271-a65a-4b94-af9a-829e8b8acc0b
found disk = 5f67d1bd-86df-4c42-ad4a-35a4456410b7
found disk = 90f13a41-b810-4857-827a-08ea3a5d605f
found disk = a2838233-2bf4-4b3f-b8f6-1017326bd018
found disk = c1f16716-dc17-43ba-b4e8-a31296e6329f
found disk = d41c3312-dc75-46ab-9549-86a1d4702fd5
Using vDisk id=167d6f3b-a74d-4e47-b2fd-30734a2f4180
Using vDisk Name=yaniv_test
going to create snapshot with name=snap-prefix-0f283baa-c274-4999-a8cb-d3aa34bb7c3a
created snapshot with id=16bbb814-0ad9-4987-b0f6-5ad11cb54d43
Creating a new vDisk with name=yaniv_test_16bbb814-0ad9-4987-b0f6-5ad11cb54d43
Created a new  vDisk with id=9176a2fc-d028-4806-9df8-ef7aaaf2ad2b
Created a new vDisk in path=[vSanDatastore] 4517895d-9ca7-8a60-f341-005056bfe40a/adb48e9f658b4db49e01c240fb1461d9.vmdk
Attaching new vDisk to VM=vm-27
Attached disk to vm in SCSI Controller=1000
Attached disk to vm in SCSI UnitNumber=10
Press Enter to continue...
```
