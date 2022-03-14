using System;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;

namespace BabelDeobfuscator.Protections
{
    public class NameCleaner
    {
        public static void CleanNames(Assembly asm, ModuleDefMD module)
        {
            List<TypeDef> typesToClean = new List<TypeDef>();

            typesToClean.AddRange(module.Types);

            foreach (TypeDef typeDef in module.GetTypes())
            {
                if (typeDef.NestedTypes.Count > 0)
                {
                    typesToClean.AddRange(typeDef.NestedTypes);
                }
            }

            int renamedTypes = 0;
            foreach (TypeDef typeDef in typesToClean)
            {
                // Find the entry point and fix the name and access modifiers
                if (!typeDef.IsNested)
                {
                    if (typeDef.Fields.Count == 0)
                    {
                        if (typeDef.Methods.Count == 1)
                        {
                            if (typeDef.Methods[0].IsStatic && typeDef.Methods[0].Name == "Main")
                            {
                                typeDef.Name = "Program";
                                typeDef.Methods[0].Access = dnlib.DotNet.MethodAttributes.Static | dnlib.DotNet.MethodAttributes.Public;

                                typeDef.Attributes = dnlib.DotNet.TypeAttributes.Public;
                            }
                        }
                    }
                }

                List<string> fieldRefNamesToChange = new List<string>();
                List<MDToken> excludedEntries = new List<MDToken>();
                string name = typeDef.Name;
                if (!isValid(name) || checkForSingleLowercaseCharName(name))
                {
                    renamedTypes++;

                    string newName = "class" + renamedTypes.ToString();
                    typeDef.Name = newName;
                }

                //if (typeDef.Name == "class1")
                //{
                //    Debugger.Break();
                //}

                // Fix generic params
                int renamedGenericParams = 0;
                if (typeDef.GenericParameters.Count > 0)
                {
                    foreach (GenericParam param in typeDef.GenericParameters)
                    {
                        if (!isValid(param.Name) || checkForSingleLowercaseCharName(param.Name))
                        {
                            renamedGenericParams++;

                            string newName = "genericParam" + renamedGenericParams.ToString();
                            param.Name = newName;
                        }
                    }
                }

                // Fix property names
                int renamedProps = 0;
                foreach (PropertyDef prop in typeDef.Properties)
                {
                    if (!isValid(prop.Name) || checkForSingleLowercaseCharName(prop.Name))
                    {
                        renamedProps++;
                        string newName = "property" + renamedProps.ToString();
                        prop.Name = newName;

                        if (prop.GetMethod != null)
                        {
                            prop.GetMethod.Name = "get_" + newName;

                        }

                        if (prop.SetMethods != null)
                        {
                            if (prop.SetMethods.Count > 0)
                            {
                                foreach (MethodDef m in prop.SetMethods)
                                {
                                    m.Name = "set_" + newName;
                                    cleanMethodParams(m);
                                }
                            }
                        }
                    }

                    // Fix property backing fields
                    if (prop.GetMethod.Body.Instructions.Count >= 3)
                    {
                        if (prop.GetMethod.Body.Instructions[0].OpCode == OpCodes.Ldarg_0 && prop.GetMethod.Body.Instructions[1].OpCode == OpCodes.Ldfld)
                        {
                            MemberRef objMember = null;
                            FieldDef fieldObj = null;
                            try
                            {
                                objMember = (MemberRef)prop.GetMethod.Body.Instructions[1].Operand;
                            }
                            catch (Exception)
                            {
                                fieldObj = (FieldDef)prop.GetMethod.Body.Instructions[1].Operand;
                            }

                            if (objMember != null)
                            {
                                FieldDef f = objMember.ResolveFieldDef();

                                if (f != null)
                                {
                                    fieldRefNamesToChange.Add(f.Name);
                                    string oldName = f.Name;
                                    f.Name = "_" + prop.Name;

                                    //foreach (FieldDef fld in typeDef.Fields)
                                    //{
                                    //    if (fld.MDToken == f.MDToken)
                                    //    {
                                    //        fld.Name = "_" + prop.Name;
                                    //        break;
                                    //    }
                                    //}

                                    Instruction ins = prop.GetMethod.Body.Instructions[1];

                                    prop.GetMethod.Body.Instructions[1] = Instruction.Create(ins.OpCode, f);

                                    excludedEntries.Add(f.MDToken);

                                    // Fix property backing field references
                                    fixFieldRefs(f, typeDef, oldName);
                                }
                            }
                            else
                            {
                                string oldName = fieldObj.ResolveFieldDef().Name;
                                fieldObj.ResolveFieldDef().Name = "_" + prop.Name;

                                Instruction ins = prop.GetMethod.Body.Instructions[1];

                                prop.GetMethod.Body.Instructions[1] = Instruction.Create(ins.OpCode, fieldObj.ResolveFieldDef());

                                excludedEntries.Add(fieldObj.MDToken);

                                // Fix property backing field references
                                fixFieldRefs(fieldObj.ResolveFieldDef(), typeDef, oldName);
                            }
                        }
                    }
                }

                //if (typeDef.Name == "class5") Debugger.Break();

                // Fix method names
                int renamedMethods = 0;
                foreach (MethodDef md in typeDef.Methods)
                {
                    if (md.Name == ".ctor" || md.Name == ".cctor")
                    {
                        cleanMethodParams(md);
                    }
                    else
                    {
                        if (!isValid(md.Name) || checkForSingleLowercaseCharName(md.Name))
                        {
                            renamedMethods++;

                            string newName = "method" + renamedMethods.ToString();
                            md.Name = newName;
                        }

                        cleanMethodParams(md);
                    }
                }

                // Fix field names
                int renamedFields = 0;
                foreach (FieldDef field in typeDef.Fields)
                {
                    bool cont = false;
                    foreach (MDToken token in excludedEntries)
                    {
                        if (field.MDToken.Equals(token))
                        {
                            cont = true;
                            break;
                        }
                    }

                    if (cont == false)
                    {
                        if (!isValid(field.Name) || checkForSingleLowercaseCharName(field.Name))
                        {
                            renamedFields++;

                            string newName = "field" + renamedFields.ToString();
                            field.Name = newName;
                        }
                    }
                }
            }
        }

        static void fixFieldRefs(FieldDef fld, TypeDef type, string oldName)
        {
            foreach (MethodDef m in type.Methods)
            {
                for (int i = 0; i < m.Body.Instructions.Count; i++)
                {
                    if (m.Body.Instructions[i].OpCode == OpCodes.Ldfld || m.Body.Instructions[i].OpCode == OpCodes.Stfld)
                    {
                        MemberRef objMember = null;
                        FieldDef fieldObj = null;
                        try
                        {
                            objMember = (MemberRef)m.Body.Instructions[i].Operand;
                        }
                        catch (Exception)
                        {
                            fieldObj = (FieldDef)m.Body.Instructions[i].Operand;
                        }

                        if (objMember != null)
                        {
                            //FieldDef f = objMember.ResolveFieldDef();

                            if (objMember.Name == oldName)
                            {
                                m.Body.Instructions[i] = Instruction.Create(m.Body.Instructions[i].OpCode, fld);
                            }
                        }
                        else
                        {
                            if (fieldObj.Name == oldName)
                            {
                                m.Body.Instructions[i] = Instruction.Create(m.Body.Instructions[i].OpCode, fld);
                            }
                        }
                        
                    }
                }
            }
        }

        static void cleanMethodParams(MethodDef md)
        {
            if (md.Parameters.Count > 0)
            {
                if (md.HasGenericParameters)
                {
                    int renamedGenericParams = 0;
                    foreach (GenericParam param in md.GenericParameters)
                    {
                        if (!isValid(param.Name) || checkForSingleLowercaseCharName(param.Name))
                        {
                            renamedGenericParams++;

                            string newName = "genericParam" + renamedGenericParams.ToString();
                            param.Name = newName;
                        }
                    }

                }
                else
                {
                    int renamedParams = 0;
                    foreach (Parameter param in md.Parameters)
                    {
                        if (param.Name == String.Empty) continue;
                        if (!isValid(param.Name) || checkForSingleLowercaseCharName(param.Name))
                        {
                            renamedParams++;
                            param.Name = "param" + renamedParams.ToString();
                        }
                    }
                }
            }
        }

        static bool checkForSingleLowercaseCharName(string str)
        {
            if (str.Length == 1)
            {
                if (str[0] >= 'a' && str[0] <= 'z')
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (str.Length > 1 && str.Length <= 3)
            {
                bool checksTrue = false;
                if (str[0] >= 'a' && str[0] <= 'z')
                {
                    for (int i = 1; i < str.Length; i++)
                    {
                        if (Char.IsLower(str[i]))
                        {
                            checksTrue = true;
                        }
                        else
                        {
                            checksTrue = false;
                            break;
                        }
                    }

                    return checksTrue;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        static bool isValid(String str)
        {
            if (str == "<Module>") return true;

            // If first character is invalid
            if (!((str[0] >= 'a' && str[0] <= 'z')
                || (str[0] >= 'A' && str[0] <= 'Z')
                || str[0] == '_'))
                return false;

            // Traverse the string for the rest of the characters
            for (int i = 1; i < str.Length; i++)
            {
                if (!((str[i] >= 'a' && str[i] <= 'z')
                    || (str[i] >= 'A' && str[i] <= 'Z')
                    || (str[i] >= '0' && str[i] <= '9')
                    || str[i] == '_'))
                    return false;
            }

            // String is a valid identifier
            return true;
        }
    }
}
