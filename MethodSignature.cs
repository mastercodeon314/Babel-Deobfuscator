using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Windows.Forms;
using MethodAttributes = dnlib.DotNet.MethodAttributes;

namespace BabelDeobfuscator.Protections
{
    public class MethodSignature
    {
        public ITypeDefOrRef ReturnType;
        public ITypeDefOrRef[] Parameters;

        public string FullName = "";
        public MDToken token;

        public int ParmCount
        {
            get
            {
                if (this.Parameters != null)
                {
                    return this.Parameters.Length;
                }

                return -1;
            }
        }

        public MethodSignature(MethodDef m)
        {
            this.ReturnType = m.ReturnType.ToTypeDefOrRef();

            if (m.Parameters.Count > 0)
            {
                List<ITypeDefOrRef> parmTypes = new List<ITypeDefOrRef>();

                for (int i = 0; i < m.Parameters.Count; i++)
                {
                    parmTypes.Add(m.Parameters[i].Type.ToTypeDefOrRef());
                }

                if (parmTypes.Count > 0)
                {
                    this.Parameters = parmTypes.ToArray();
                }
            }
            else
            {
                this.Parameters = null;
            }

            this.FullName = m.FullName;
            this.token = m.MDToken;
        }

        public MethodSignature(ITypeDefOrRef returnType, ITypeDefOrRef[] argTypes)
        {
            this.ReturnType = returnType;
            this.Parameters = argTypes;
        }

        public MethodSignature(ITypeDefOrRef returnType)
        {
            this.ReturnType = returnType;
            this.Parameters = null;
        }

        public MethodSignature(ITypeDefOrRef returnType, ITypeDefOrRef argType)
        {
            this.ReturnType = returnType;
            this.Parameters = new ITypeDefOrRef[] { argType };
        }

        public static bool Equals(MethodSignature a, MethodSignature b)
        {

            bool res = false;
            try
            {
                res = Utils.typesEqual(a.ReturnType, b.ReturnType);

                if (res == true)
                {
                    res = a.ParmCount == b.ParmCount;

                    if (res == true)
                    {
                        if (a.Parameters != null && b.Parameters != null)
                        {
                            if (a.Parameters.Length == b.Parameters.Length)
                            {
                                for (int i = 0; i < a.ParmCount; i++)
                                {
                                    res = Utils.typesEqual(a.Parameters[i], b.Parameters[i]);

                                    if (res == false) return false;
                                }
                            }
                        }
                    }

                    if (res == true && a.FullName != "" && a.token != null && b.FullName != "" && b.token != null)
                    {
                        res = a.FullName == b.FullName && a.token.Equals(b.token);
                        if (res == false) return false;
                    }
                }
            }
            catch (Exception)
            {
                return res;
            }

            return res;
        }


        public override bool Equals(object obj)
        {
            bool res = false;
            try
            {
                MethodSignature m = (MethodSignature)obj;

                if (m != null)
                {
                    try
                    {
                        res = Utils.typesEqual(this.ReturnType, m.ReturnType);

                    }
                    catch (Exception) { }


                    if (res == true)
                    {
                        res = this.ParmCount == m.ParmCount;

                        if (res == true)
                        {
                            if (this.Parameters != null && m.Parameters != null)
                            {
                                if (this.Parameters.Length == m.Parameters.Length)
                                {
                                    for (int i = 0; i < this.ParmCount; i++)
                                    {
                                        try
                                        {
                                            res = Utils.typesEqual(this.Parameters[i], m.Parameters[i]);

                                        }
                                        catch (Exception) { }


                                        if (res == false) return false;
                                    }
                                }
                            }

                        }

                        if (res == true && this.FullName != "" && this.token != null && m.FullName != "" && m.token != null)
                        {
                            res = this.FullName == m.FullName && this.token.Equals(m.token);
                            if (res == false) return false;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return res;
            }

            return res;
        }
    }
}