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

Setting Up Development Environment
* vSphere 6.7 Update 3 
* vSphere 6.7 Update 3 DotNet SDK - [Download](https://my.vmware.com/group/vmware/get-download?downloadGroup=VS-MGMT-SDK67U3)
* Visual Studio 2015 Community Edition

## Running
```shell
fcdSampleCode 1.0.0.0
Copyright c  2019

  -s, --server       Required. vCenter SDK Url in https://<vcenter>/sdk

  -u, --username     Required. Username for vCenter Connection

  -p, --password     Required. password for vCenter Connection

  -d, --datastore    Required. Datastore name

  -f, --fcd          Required. Existing First Class Disk Name

  -v, --vm           Required. Target VM Name

  -n, --snapshot     Required. Snapshot Name Prefix, i.e. snap-

  --help             Display this help screen.

  --version          Display version information.

```

Sample Command
```shell
fcdSampleCode.exe -s "https://yaniv-vcsa-01a.pso-il.local/sdk" -u "administrator@vsphere.local" -p "VMware1!" -d "vsanDatastore" -f "yaniv_test" -v "centos" -n "snap-prefix-"
```

Example Output
```shell
[ 9/24/2019 10:27:29 PM ] Begin Log.
SCSI Controller=1000
SCSI UnitNumber=8
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
going to create snapshot with name=snap-prefix-0b64e0fd-3a7d-4714-ac38-a23ab4f0ca5c
created snapshot with id=5e06af01-d9cf-4fef-a810-c142ee7cc471
Creating a new vDisk with name=yaniv_test_5e06af01-d9cf-4fef-a810-c142ee7cc471
created a new  vDisk with id=6accdcc4-4823-496f-b512-c8396fa587d2
created a new vDisk in path=[vsanDatastore] 4517895d-9ca7-8a60-f341-005056bfe40a/98c37affccac462ea6a2588391e8b412.vmdk
Attaching new vDisk to VM=vm-27
Attached disk to vm in SCSI Controller=1000
Attached disk to vm in SCSI UnitNumber=8
Press Enter to continue...
```
