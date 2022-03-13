using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace BabelDeobfuscator.Protections
{
    public class Utils
    {
        public static MethodDef findFormInit(TypeDef frmType)
        {
            MethodDef constr = frmType.FindDefaultConstructor();

            if (constr == null)
            {
                IEnumerable<MethodDef> defs = (IEnumerable<MethodDef>)frmType.FindConstructors();

                if (defs.Count() > 0)
                {
                    constr = defs.ToArray()[0];
                }
            }

            Instruction[] opCodes = constr.Body.Instructions.ToArray();

            for (int i = 0; i < opCodes.Length; i++)
            {
                if (opCodes[i].OpCode == OpCodes.Call)
                {
                    IMethodDefOrRef me = (IMethodDefOrRef)opCodes[i].Operand;

                    if (me.DeclaringType.FullName == frmType.FullName)
                    {
                        me.Name = "InitializeComponent";
                        return me.ResolveMethodDef();
                    }
                }
            }

            return null;
        }

        public static void fixFormEventMethods(TypeDef frmType)
        {
            MethodDef frmInit = findFormInit(frmType);

            Instruction[] opCodes = frmInit.Body.Instructions.ToArray();

            for (int i = 0; i < opCodes.Length; i++)
            {
                if (opCodes[i].OpCode == OpCodes.Ldftn)
                {
                    if (opCodes[i - 2].OpCode == OpCodes.Ldfld)
                    {
                        if (opCodes[i + 2].OpCode == OpCodes.Callvirt)
                        {
                            IField fld = (IField)opCodes[i - 2].Operand;
                            IMethodDefOrRef eventAddMethod = ((IMethodDefOrRef)opCodes[i + 2].Operand).ResolveMethodDef();
                            IMethodDefOrRef eventMethod = ((IMethodDefOrRef)opCodes[i].Operand).ResolveMethodDef();
                            string eventAddName = eventAddMethod.Name.Replace("add_", "");
                            string eventName = eventMethod.Name;
                            string fldName = fld.Name;

                            eventMethod.Name = fldName + "_" + eventAddName;
                        }
                    }
                }
            }

        }

        public static bool isControlField(FieldInfo inf)
        {
            bool result = false;

            string controlFullName = typeof(System.Windows.Forms.Control).FullName;
            string toolStripItemFullName = typeof(System.Windows.Forms.ToolStripItem).FullName;

            Type baseType = null;

            if (inf.FieldType.Module.FullyQualifiedName != typeof(System.Windows.Forms.Control).Module.FullyQualifiedName) return false;

            try
            {
                baseType = inf.FieldType.BaseType;
            }
            catch (Exception)
            {

            }

            if (baseType == null) return false;

            if (baseType.FullName == controlFullName)
            {
                return true;
            }
            else
            {
                while (baseType.FullName != controlFullName)
                {
                    if (baseType.BaseType == null)
                    {
                        return false;
                    }
                    else
                    {
                        baseType = baseType.BaseType;
                    }
                }

                if (baseType.FullName == controlFullName)
                {
                    return true;
                }
            }

            return result;
        }

        public static bool isToolStripItem(FieldInfo inf)
        {
            bool result = false;

            string toolStripItemFullName = typeof(System.Windows.Forms.ToolStripItem).FullName;

            Type baseType = null;

            if (inf.FieldType.Module.FullyQualifiedName != typeof(System.Windows.Forms.Control).Module.FullyQualifiedName) return false;

            try
            {
                baseType = inf.FieldType.BaseType;
            }
            catch (Exception)
            {

            }

            if (baseType == null) return false;

            if (baseType.FullName == toolStripItemFullName)
            {
                return true;
            }
            else
            {
                while (baseType.FullName != toolStripItemFullName)
                {
                    if (baseType.BaseType == null)
                    {
                        return false;
                    }
                    else
                    {
                        baseType = baseType.BaseType;
                    }
                }

                if (baseType.FullName == toolStripItemFullName)
                {
                    return true;
                }
            }

            return result;
        }

        public static bool typesEqual(TypeSig a, TypeSig b)
        {
            return a.ReflectionFullName.Equals(b.ReflectionFullName);
        }

        public static bool typesEqual(ITypeDefOrRef a, TypeSig b)
        {
            return a.ReflectionFullName.Equals(b.ReflectionFullName);
        }

        public static bool typesEqual(TypeSig a, ITypeDefOrRef b)
        {
            return a.ReflectionFullName.Equals(b.ReflectionFullName);
        }

        public static bool typesEqual(ITypeDefOrRef a, ITypeDefOrRef b)
        {
            return a.ReflectionFullName.Equals(b.ReflectionFullName);
        }

        public static ITypeDefOrRef reflectionType_To_dnType(Type t, ModuleDefMD asm, ModuleContext modCtx)
        {
            ITypeDefOrRef res = null;

            if (t.Module.FullyQualifiedName == asm.Location)
            {
                try
                {
                    Importer imp = new Importer(asm);
                    ITypeDefOrRef tt = imp.Import(t);

                    if (tt != null)
                    {
                        res = tt;
                        return res;
                    }
                }
                catch (Exception)
                {
                    Debugger.Break();
                }
            }
            else
            {
                try
                {
                    ModuleDefMD mod = ModuleDefMD.Load(t.Module, modCtx);
                    Importer imp = new Importer(mod);
                    ITypeDefOrRef tt = imp.Import(t);

                    if (tt != null)
                    {
                        res = tt;
                        return res;
                    }

                }
                catch (Exception)
                {
                    Debugger.Break();
                }
            }

            return res;
        }
    }
}