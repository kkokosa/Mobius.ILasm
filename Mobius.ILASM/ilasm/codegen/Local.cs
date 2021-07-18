//
// Mono.ILASM.Local
//
// Author(s):
//  Jackson Harper (Jackson@LatitudeGeo.com)
//
// (C) 2003 Jackson Harper, All rights reserved
//


using Mobius.ILasm.infrastructure;
using System;
using System.Collections.Generic;


namespace Mono.ILASM
{

    public class Local
    {

        private int slot;
        private string name;
        private BaseTypeRef type;
        private Dictionary<string, string> errors;

        public Local(int slot, BaseTypeRef type, Dictionary<string, string> errors) : this(slot, null, type, errors)
        {

        }

        public Local(int slot, string name, BaseTypeRef type, Dictionary<string, string> errors)
        {
            this.slot = slot;
            this.name = name;
            this.type = type;
            this.errors = errors;
        }

        public int Slot
        {
            get { return slot; }
            set { slot = value; }
        }

        public string Name
        {
            get { return name; }
        }

        public BaseTypeRef Type
        {
            get { return type; }
        }

        public PEAPI.Local GetPeapiLocal(CodeGen code_gen)
        {
            int ec = errors.Count;
            BaseGenericTypeRef gtr = type as BaseGenericTypeRef;
            if (gtr == null)
                type.Resolve(code_gen);
            else
                gtr.ResolveNoTypeSpec(code_gen);

            if (errors.Count > ec)
                return null;

            return new PEAPI.Local(name, type.PeapiType);
        }
    }

}

