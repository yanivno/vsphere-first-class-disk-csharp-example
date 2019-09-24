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

using System.IO;

namespace VMware.Security.CredentialStore {
   /// <summary>
   /// Factory class providing instances of a <see cref="ICredentialStore"/>
   /// credential store.
   /// </summary>
   public class CredentialStoreFactory {
      /// <summary>
      /// Returns the default credential store. If the file backing the
      /// credential store does not exist, it is created (along with its
      /// directory if needed).
      /// </summary>
      /// <exception cref="IOException"/>
      static public ICredentialStore CreateCredentialStore() {
         return new CredentialStore();
      }

      /// <summary>
      /// Returns a credential store given the file backing it. If <code>file
      /// </code> is <code>null</code> this method acts as
      /// <see cref="CreateCredentialStore()"/>. Otherwise, the specified file
      /// (but not its directory) is created if it does not already exist.
      /// </summary>
      /// <param name="file">The file to use, or <code>null</code> to use the
      /// default</param>
      /// <returns>The credential store for the specified file</returns>
      /// <exception cref="IOException"/>
      static public ICredentialStore CreateCredentialStore(FileInfo file) {
         if (file == null) {
            return CreateCredentialStore();
         }

         if (file.Directory.Exists) {
            return new CredentialStore(file);
         } else {
            throw new DirectoryNotFoundException(file.Directory.FullName);
         }
      }
   }
}