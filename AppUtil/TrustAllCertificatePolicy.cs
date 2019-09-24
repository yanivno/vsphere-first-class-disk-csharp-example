/*
 * ******************************************************
 * Copyright (c) VMware, Inc. 2010.  All Rights Reserved.
 * ******************************************************
 *
 * DISCLAIMER. THIS PROGRAM IS PROVIDED TO YOU "AS IS" WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, WHETHER ORAL OR WRITTEN,
 * EXPRESS OR IMPLIED. THE AUTHOR SPECIFICALLY DISCLAIMS ANY IMPLIED
 * WARRANTIES OR CONDITIONS OF MERCHANTABILITY, SATISFACTORY QUALITY,
 * NON-INFRINGEMENT AND FITNESS FOR A PARTICULAR PURPOSE.
 */

using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Collections;


namespace AppUtil
{
    public class TrustAllCertificatePolicy : System.Net.ICertificatePolicy
    {
        public TrustAllCertificatePolicy() { }
        public bool CheckValidationResult(ServicePoint sp,
            X509Certificate cert,
            WebRequest req,
            int problem)
        {
            return true;
        }
    }

}