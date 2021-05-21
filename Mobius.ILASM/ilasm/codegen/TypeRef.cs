//
// Mono.ILASM.TypeRef
//
// Author(s):
//  Jackson Harper (Jackson@LatitudeGeo.com)
//
// (C) 2003 Jackson Harper, All rights reserved
//

using Mobius.ILasm.infrastructure;
using Mobius.ILasm.interfaces;
using System;
using System.Collections;
using System.Diagnostics;

namespace Mono.ILASM
{

    /// <summary>
    /// Reference to a type in the module being compiled.
    /// </summary>
    public class TypeRef : BaseClassRef
    {

        private Location location;
        private ILogger logger;
        public static readonly TypeRef Ellipsis = new TypeRef(null, "ELLIPSIS", false, null);
        public static readonly TypeRef Any = new TypeRef(null, "any", false, null);
            
        public TypeRef(ILogger logger, string full_name, bool is_valuetype, Location location)
                : this(logger, full_name, is_valuetype, location, null, null)
        {
            this.logger = logger;
        }

        public TypeRef(ILogger logger, string full_name, bool is_valuetype, Location location, ArrayList conv_list, string sig_mod)
                : base(full_name, is_valuetype, conv_list, sig_mod)
        {
            this.location = location;
            this.logger = logger;
        }

        public override BaseTypeRef Clone()
        {
            return new TypeRef(logger, full_name, is_valuetype, location, (ArrayList)ConversionList.Clone(), sig_mod);
        }

        protected override BaseMethodRef CreateMethodRef(BaseTypeRef ret_type,
                PEAPI.CallConv call_conv, string name, BaseTypeRef[] param, int gen_param_count)
        {
            if (SigMod == null | SigMod == "")
                return new MethodRef(this, call_conv, ret_type, name, param, gen_param_count);
            else
                return new TypeSpecMethodRef(this, call_conv, ret_type, name, param, gen_param_count);
        }

        protected override IFieldRef CreateFieldRef(BaseTypeRef ret_type, string name)
        {
            return new FieldRef(this, ret_type, name);
        }

        public override void Resolve(CodeGen code_gen)
        {
            if (is_resolved)
                return;

            PEAPI.Type base_type;

            base_type = code_gen.TypeManager.GetPeapiType(full_name);

            if (base_type == null)
            {
                logger?.Error("Reference to undefined class '" +
                                       FullName + "'");
                FileProcessor.ErrorCount += 1;
                Debug.Assert(logger != null);
                return;
            }
            type = Modify(code_gen, base_type);

            is_resolved = true;
        }

        public BaseClassRef AsClassRef(CodeGen code_gen)
        {
            return this;
        }

        public override string ToString()
        {
            return FullName;
        }

    }

}

