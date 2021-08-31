//
// Mono.ILASM.FieldRef
//
// Author(s):
//  Jackson Harper (Jackson@LatitudeGeo.com)
//
// (C) 2003 Jackson Harper, All rights reserved
//


using System;
using System.Collections.Generic;

namespace Mono.ILASM {


        public class FieldRef : IFieldRef {

                private TypeRef owner;
                private BaseTypeRef ret_type;
                private string name;

        private bool is_resolved;
                private PEAPI.Field peapi_field;

                public FieldRef (TypeRef owner, BaseTypeRef ret_type, string name)
                {
                        this.owner = owner;
                        this.ret_type = ret_type;
                        this.name = name;
			
			is_resolved = false;
                }

                public PEAPI.Field PeapiField {
                        get { return peapi_field; }
                }

                public void Resolve (CodeGen code_gen)
                {
			if (is_resolved)
				return;

                        TypeDef owner_def = code_gen.TypeManager[owner.FullName];
                        // TODO: Consider better message that contain context (method name?)
                        if (owner_def is null)
                            throw new Exception($"Unable to find type {owner.FullName}");
                        peapi_field = owner_def.ResolveField (name, ret_type, code_gen);

			is_resolved = true;
                }
        }
}

