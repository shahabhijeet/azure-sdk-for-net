// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.

using Microsoft.Extensions.DependencyModel;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
#if !FullNetFx
using System.Runtime.Loader;
#endif


namespace Microsoft.Azure
{
    static internal class NativeMethods
    {
        const int AssemblyPathMax = 1024;
        
        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("e707dcde-d1cd-11d2-bab9-00c04f8eceae")]
        private interface IAssemblyCache
        {
            void Reserved0();

            [PreserveSig]
            int QueryAssemblyInfo(int flags, [MarshalAs(UnmanagedType.LPWStr)] string assemblyName, ref AssemblyInfo assemblyInfo);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AssemblyInfo
        {
            public int cbAssemblyInfo;

            public int assemblyFlags;

            public long assemblySizeInKB;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string currentAssemblyPath;

            public int cchBuffer; // size of path buffer.
        }

        [DllImport("fusion.dll")]
        private static extern int CreateAssemblyCache(out IAssemblyCache ppAsmCache, int reserved);

        /// <summary>
        /// Gets an assembly path from the GAC given a partial name.
        /// </summary>
        /// <param name="name">An assembly partial name. May not be null.</param>
        /// <returns>
        /// The assembly path if found; otherwise null;
        /// </returns>
        public static string GetAssemblyPath(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            string finalName = name;
            AssemblyInfo aInfo = new AssemblyInfo();
            aInfo.cchBuffer = AssemblyPathMax;
            aInfo.currentAssemblyPath = new String('\0', aInfo.cchBuffer);

            IAssemblyCache ac;
            int hr = CreateAssemblyCache(out ac, 0);
            if (hr >= 0)
            {
                hr = ac.QueryAssemblyInfo(0, finalName, ref aInfo);
                if (hr < 0)
                    return null;
            }

            return aInfo.currentAssemblyPath;
        }
#if !FullNetFx
        public static Assembly LoadFromAssemblyContext(string assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName)) throw new NullReferenceException(string.Format(Resources.ErrorArgumentEmptyString, "assemblyName"));

            AssemblyLoader asmLoad = new AssemblyLoader();
            AssemblyName asmName = new AssemblyName(assemblyName);
            Assembly assembly = asmLoad.LoadFromAssemblyName(asmName);
            return assembly;
        }
#endif
    }

#if !FullNetFx
    public class AssemblyLoader : AssemblyLoadContext
    {
        protected override Assembly Load(AssemblyName assemblyName)
        {
            var deps = DependencyContext.Default;
            var res = deps.CompileLibraries.Where(d => d.Name.Contains(assemblyName.Name)).ToList();
            var assembly = Assembly.Load(new AssemblyName(res.First().Name));
            return assembly;
        }
    }
#endif
}
