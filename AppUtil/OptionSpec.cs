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
    public class OptionSpec
    {
        private string optionName;
        private int optionRequired;
        private string optionDesc;
        private string optionType;
        private string optionDefault;

        public OptionSpec(string optionName, string optionType, int optionRequired, string optionDesc, string optionDefault)
        {
            this.optionName = optionName;
            this.optionType = optionType;
            this.optionRequired = optionRequired;
            this.optionDesc = optionDesc;
            this.optionDefault = optionDefault;
        }

        public string getOptionName()
        {
            return optionName;
        }

        public int getOptionRequired()
        {
            return optionRequired;
        }

        public string getOptionDesc()
        {
            return optionDesc;
        }

        public string getOptionType()
        {
            return optionType;
        }

        public string getOptionDefault()
        {
            return optionDefault;
        }
    }
}
