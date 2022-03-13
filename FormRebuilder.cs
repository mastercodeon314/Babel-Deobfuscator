using System;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace BabelDeobfuscator.Protections
{
    public class FormRebuilder
    {
        public static void RebuildForms(Assembly asm, ModuleDefMD module)
        {
            Type delegateBaseType = typeof(System.MulticastDelegate);
            Type formBaseType = typeof(System.Windows.Forms.Form);
            Type controlBaseType = typeof(System.Windows.Forms.Control);
            ModuleDefMD m = ModuleDefMD.Load(delegateBaseType.Module);
            ModuleDefMD winformModule = ModuleDefMD.Load(formBaseType.Module);
            ITypeDefOrRef MulticastDelegateType = Utils.reflectionType_To_dnType(delegateBaseType, m, m.Context);
            ITypeDefOrRef formBaseTypeRef = Utils.reflectionType_To_dnType(formBaseType, winformModule, winformModule.Context);
            ITypeDefOrRef controlBaseTypeRef = Utils.reflectionType_To_dnType(controlBaseType, winformModule, winformModule.Context);

            foreach (TypeDef typeDef in module.GetTypes())
            {
                if (typeDef == null || typeDef.BaseType == null) continue;

                if (typeDef.BaseType.FullName == formBaseTypeRef.FullName)
                {
                    Type reflectionFormType = asm.GetType(typeDef.ReflectionFullName);

                    if (reflectionFormType == null)
                    {
                        continue;
                    }

                    object frm = null;
                    try
                    {
                        frm = Activator.CreateInstance(reflectionFormType);
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message == "No parameterless constructor defined for this object.")
                        {
                            frm = Activator.CreateInstance(reflectionFormType, new object[] { null });
                        }
                    }

                    if (frm != null)
                    {
                        MethodDef frmInit = Utils.findFormInit(typeDef);
                        Type frmTP = frm.GetType();
                        FieldInfo[] infs = frmTP.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                        Dictionary<string, string> conversionMap = new Dictionary<string, string>();


                        foreach (var reField in infs)
                        {
                            if (Utils.isControlField(reField))
                            {
                                Control ctrl = null;

                                try
                                {
                                    ctrl = (Control)reField.GetValue(frm);
                                }

                                catch (Exception) { }

                                if (ctrl != null) conversionMap.Add(reField.Name, ctrl.Name);
                            }

                            if (Utils.isToolStripItem(reField))
                            {
                                ToolStripItem item = null;

                                try
                                {
                                    item = (ToolStripItem)reField.GetValue(frm);
                                }

                                catch (Exception) { }

                                if (item != null) conversionMap.Add(reField.Name, item.Name);
                            }
                        }


                        foreach (var field in typeDef.Fields)
                        {
                            if (conversionMap.ContainsKey(field.Name) == true)
                            {
                                field.Name = conversionMap[field.Name];
                            }
                        }

                        Utils.fixFormEventMethods(typeDef);
                    }
                }
            }
        }
    }
}
