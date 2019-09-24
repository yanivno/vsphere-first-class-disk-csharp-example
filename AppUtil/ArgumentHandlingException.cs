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

namespace AppUtil
{
    public class ArgumentHandlingException : Exception
    {
        public ArgumentHandlingException(String msg)
            : base(msg)
        {
            Console.WriteLine(msg);
            Console.Write("Press Enter Key To Exit: ");
            Console.ReadLine();
            Environment.Exit(1);

        }
    }
}
