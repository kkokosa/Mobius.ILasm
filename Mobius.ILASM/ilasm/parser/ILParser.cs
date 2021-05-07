// created by jay 0.7 (c) 1998 Axel.Schreiner@informatik.uni-osnabrueck.de

#line 2 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
//
// Mono::ILASM::ILParser
// 
// (C) Sergey Chaban (serge@wildwestsoftware.com)
// (C) 2003 Jackson Harper, All rights reserved
//

using PEAPI;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

using MIPermission = Mono.ILASM.Permission;
using MIPermissionSet = Mono.ILASM.PermissionSet;
using Mobius.ILasm.interfaces;
using Mobius.ILasm.infrastructure;

namespace Mono.ILASM
{

    public class ILParser
    {

        private CodeGen codegen;

        private bool is_value_class;
        private bool is_enum_class;
        private bool pinvoke_info;
        private string pinvoke_mod;
        private string pinvoke_meth;
        private PEAPI.PInvokeAttr pinvoke_attr;
        private ILTokenizer tokenizer;
        const int yacc_verbose_flag = 0;
        KeyValuePair<string, TypeAttr> current_extern;
        private readonly ILog logger;

        class NameValuePair
        {
            public string Name;
            public object Value;

            public NameValuePair(string name, object value)
            {
                this.Name = name;
                this.Value = value;
            }
        }

        class PermPair
        {
            public PEAPI.SecurityAction sec_action;
            public object perm;

            public PermPair(PEAPI.SecurityAction sec_action, object perm)
            {
                this.sec_action = sec_action;
                this.perm = perm;
            }
        }

        public bool CheckSecurityActionValidity(System.Security.Permissions.SecurityAction action, bool for_assembly)
        {
#pragma warning disable 618
            if ((action == System.Security.Permissions.SecurityAction.RequestMinimum ||
                    action == System.Security.Permissions.SecurityAction.RequestOptional ||
                    action == System.Security.Permissions.SecurityAction.RequestRefuse) && !for_assembly)
            {
                logger.Warning(String.Format("System.Security.Permissions.SecurityAction '{0}' is not valid for this declaration", action));
                return false;
            }
#pragma warning restore 618
            return true;
        }

        public void AddSecDecl(object perm, bool for_assembly)
        {
            PermPair pp = perm as PermPair;

            if (pp == null)
            {
                MIPermissionSet ps_20 = (MIPermissionSet)perm;
                codegen.AddPermission(ps_20.SecurityAction, ps_20);
                return;
            }

            if (!CheckSecurityActionValidity((System.Security.Permissions.SecurityAction)pp.sec_action, for_assembly))

            {
                logger.Error(String.Format("Invalid security action : {0}", pp.sec_action));
                FileProcessor.ErrorCount += 1;
            }

            codegen.AddPermission(pp.sec_action, pp.perm);
        }

        public object ClassRefToObject(object class_ref, object val)
        {
            ExternTypeRef etr = class_ref as ExternTypeRef;
            if (etr == null)
                /* FIXME: report error? can be PrimitiveTypeRef or TypeRef */
                return null;

            System.Type t = etr.GetReflectedType();
            return (t.IsEnum ? Enum.Parse(t, String.Format("{0}", val)) : val);
        }

        /* Converts a type_spec to a corresponding PermPair */
        PermPair TypeSpecToPermPair(object action, object type_spec, ArrayList pairs)
        {
            ExternTypeRef etr = type_spec as ExternTypeRef;
            if (etr == null)
                /* FIXME: could be PrimitiveTypeRef or TypeRef 
                          Report what error? */
                return null;

            System.Type t = etr.GetReflectedType();
            object obj = Activator.CreateInstance(t,
                                    new object[] { (System.Security.Permissions.SecurityAction)(short)action });

            if (pairs != null)
                foreach (NameValuePair pair in pairs)
                {
                    PropertyInfo pi = t.GetProperty(pair.Name);
                    pi.SetValue(obj, pair.Value, null);
                }

            IPermission iper = (IPermission)t.GetMethod("CreatePermission").Invoke(obj, null);
            return new PermPair((PEAPI.SecurityAction)action, iper);
        }

        public ILParser(CodeGen codegen, ILTokenizer tokenizer, ILog logger)
        {
            this.codegen = codegen;
            this.tokenizer = tokenizer;
            this.logger = logger;
        }

        public CodeGen CodeGen
        {
            get { return codegen; }
        }

        private BaseTypeRef GetTypeRef(BaseTypeRef b)
        {
            //FIXME: Caching required.. 
            return b.Clone();
        }

#line default

        /** error output stream.
            It should be changeable.
          */
        public System.IO.TextWriter ErrorOutput = System.Console.Out;

        /** simplified error message.
            @see <a href="#yyerror(java.lang.String, java.lang.String[])">yyerror</a>
          */
        public void yyerror(string message)
        {
            yyerror(message, null);
        }
#pragma warning disable 649
        /* An EOF token */
        public int eof_token;
#pragma warning restore 649
        /** (syntax) error message.
            Can be overwritten to control message format.
            @param message text to be displayed.
            @param expected vector of acceptable tokens, if available.
          */
        public void yyerror(string message, string[] expected)
        {
            if ((yacc_verbose_flag > 0) && (expected != null) && (expected.Length > 0))
            {
                ErrorOutput.Write(message + ", expecting");
                for (int n = 0; n < expected.Length; ++n)
                    ErrorOutput.Write(" " + expected[n]);
                ErrorOutput.WriteLine();
            }
            else
                ErrorOutput.WriteLine(message);
        }

        /** debugging support, requires the package jay.yydebug.
            Set to null to suppress debugging messages.
          */
        internal yydebug.yyDebug debug;

        protected const int yyFinal = 1;
        // Put this array into a separate class so it is only initialized if debugging is actually used
        // Use MarshalByRefObject to disable inlining
        class YYRules : MarshalByRefObject
        {
            public static readonly string[] yyRule = {
    "$accept : il_file",
    "il_file : decls",
    "decls :",
    "decls : decls decl",
    "decl : class_all",
    "decl : namespace_all",
    "decl : method_all",
    "decl : field_decl",
    "decl : data_decl",
    "decl : vtfixup_decl",
    "decl : file_decl",
    "decl : assembly_all",
    "decl : assemblyref_all",
    "decl : exptype_all",
    "decl : manifestres_all",
    "decl : module_head",
    "decl : sec_decl",
    "decl : customattr_decl",
    "decl : D_SUBSYSTEM int32",
    "decl : D_CORFLAGS int32",
    "decl : D_FILE K_ALIGNMENT int32",
    "decl : D_IMAGEBASE int64",
    "decl : D_STACKRESERVE int64",
    "decl : extsource_spec",
    "decl : language_decl",
    "extsource_spec : D_LINE int32 SQSTRING",
    "extsource_spec : D_LINE int32",
    "extsource_spec : D_LINE int32 COLON int32 SQSTRING",
    "extsource_spec : D_LINE int32 COLON int32",
    "language_decl : D_LANGUAGE SQSTRING",
    "language_decl : D_LANGUAGE SQSTRING COMMA SQSTRING",
    "language_decl : D_LANGUAGE SQSTRING COMMA SQSTRING COMMA SQSTRING",
    "vtfixup_decl : D_VTFIXUP OPEN_BRACKET int32 CLOSE_BRACKET vtfixup_attr K_AT id",
    "vtfixup_attr :",
    "vtfixup_attr : vtfixup_attr K_INT32",
    "vtfixup_attr : vtfixup_attr K_INT64",
    "vtfixup_attr : vtfixup_attr K_FROMUNMANAGED",
    "vtfixup_attr : vtfixup_attr K_CALLMOSTDERIVED",
    "namespace_all : namespace_head OPEN_BRACE decls CLOSE_BRACE",
    "namespace_head : D_NAMESPACE comp_name",
    "class_all : class_head OPEN_BRACE class_decls CLOSE_BRACE",
    "class_head : D_CLASS class_attr comp_name formal_typars_clause extends_clause impl_clause",
    "class_attr :",
    "class_attr : class_attr K_PUBLIC",
    "class_attr : class_attr K_PRIVATE",
    "class_attr : class_attr K_NESTED K_PRIVATE",
    "class_attr : class_attr K_NESTED K_PUBLIC",
    "class_attr : class_attr K_NESTED K_FAMILY",
    "class_attr : class_attr K_NESTED K_ASSEMBLY",
    "class_attr : class_attr K_NESTED K_FAMANDASSEM",
    "class_attr : class_attr K_NESTED K_FAMORASSEM",
    "class_attr : class_attr K_VALUE",
    "class_attr : class_attr K_ENUM",
    "class_attr : class_attr K_INTERFACE",
    "class_attr : class_attr K_SEALED",
    "class_attr : class_attr K_ABSTRACT",
    "class_attr : class_attr K_AUTO",
    "class_attr : class_attr K_SEQUENTIAL",
    "class_attr : class_attr K_EXPLICIT",
    "class_attr : class_attr K_ANSI",
    "class_attr : class_attr K_UNICODE",
    "class_attr : class_attr K_AUTOCHAR",
    "class_attr : class_attr K_IMPORT",
    "class_attr : class_attr K_SERIALIZABLE",
    "class_attr : class_attr K_BEFOREFIELDINIT",
    "class_attr : class_attr K_SPECIALNAME",
    "class_attr : class_attr K_RTSPECIALNAME",
    "extends_clause :",
    "extends_clause : K_EXTENDS generic_class_ref",
    "impl_clause :",
    "impl_clause : impl_class_refs",
    "impl_class_refs : K_IMPLEMENTS generic_class_ref",
    "impl_class_refs : impl_class_refs COMMA generic_class_ref",
    "formal_typars_clause :",
    "formal_typars_clause : OPEN_ANGLE_BRACKET formal_typars CLOSE_ANGLE_BRACKET",
    "typars_clause :",
    "typars_clause : OPEN_ANGLE_BRACKET typars CLOSE_ANGLE_BRACKET",
    "typars : type",
    "typars : typars COMMA type",
    "constraints_clause :",
    "constraints_clause : OPEN_PARENS constraints CLOSE_PARENS",
    "constraints : type",
    "constraints : constraints COMMA type",
    "generic_class_ref : class_ref",
    "generic_class_ref : K_OBJECT",
    "generic_class_ref : K_CLASS class_ref typars_clause",
    "generic_class_ref : BANG int32",
    "generic_class_ref : BANG BANG int32",
    "generic_class_ref : BANG id",
    "generic_class_ref : BANG BANG id",
    "formal_typars : formal_typar_attr constraints_clause formal_typar",
    "formal_typars : formal_typars COMMA formal_typar_attr constraints_clause formal_typar",
    "formal_typar_attr :",
    "formal_typar_attr : formal_typar_attr PLUS",
    "formal_typar_attr : formal_typar_attr DASH",
    "formal_typar_attr : formal_typar_attr D_CTOR",
    "formal_typar_attr : formal_typar_attr K_VALUETYPE",
    "formal_typar_attr : formal_typar_attr K_CLASS",
    "formal_typar : id",
    "param_type_decl : D_PARAM K_TYPE id",
    "param_type_decl : D_PARAM K_TYPE OPEN_BRACKET int32 CLOSE_BRACKET",
    "class_refs : class_ref",
    "class_refs : class_refs COMMA class_ref",
    "slashed_name : comp_name",
    "slashed_name : slashed_name SLASH comp_name",
    "class_ref : OPEN_BRACKET slashed_name CLOSE_BRACKET slashed_name",
    "class_ref : OPEN_BRACKET D_MODULE slashed_name CLOSE_BRACKET slashed_name",
    "class_ref : slashed_name",
    "class_decls :",
    "class_decls : class_decls class_decl",
    "class_decl : method_all",
    "class_decl : class_all",
    "class_decl : event_all",
    "class_decl : prop_all",
    "class_decl : field_decl",
    "class_decl : data_decl",
    "class_decl : sec_decl",
    "class_decl : extsource_spec",
    "class_decl : customattr_decl",
    "class_decl : param_type_decl",
    "class_decl : D_SIZE int32",
    "class_decl : D_PACK int32",
    "$$1 :",
    "class_decl : D_OVERRIDE type_spec DOUBLE_COLON method_name K_WITH call_conv type type_spec DOUBLE_COLON method_name type_list $$1 OPEN_PARENS sig_args CLOSE_PARENS",
    "class_decl : language_decl",
    "type : generic_class_ref",
    "type : K_VALUE K_CLASS class_ref",
    "type : K_VALUETYPE OPEN_BRACKET slashed_name CLOSE_BRACKET slashed_name typars_clause",
    "type : K_VALUETYPE slashed_name typars_clause",
    "type : type OPEN_BRACKET CLOSE_BRACKET",
    "type : type OPEN_BRACKET bounds CLOSE_BRACKET",
    "type : type AMPERSAND",
    "type : type STAR",
    "type : type K_PINNED",
    "type : type K_MODREQ OPEN_PARENS custom_modifier_type CLOSE_PARENS",
    "type : type K_MODOPT OPEN_PARENS custom_modifier_type CLOSE_PARENS",
    "type : K_METHOD call_conv type STAR OPEN_PARENS sig_args CLOSE_PARENS",
    "type : primitive_type",
    "primitive_type : K_INT8",
    "primitive_type : K_INT16",
    "primitive_type : K_INT32",
    "primitive_type : K_INT64",
    "primitive_type : K_FLOAT32",
    "primitive_type : K_FLOAT64",
    "primitive_type : K_UNSIGNED K_INT8",
    "primitive_type : K_UINT8",
    "primitive_type : K_UNSIGNED K_INT16",
    "primitive_type : K_UINT16",
    "primitive_type : K_UNSIGNED K_INT32",
    "primitive_type : K_UINT32",
    "primitive_type : K_UNSIGNED K_INT64",
    "primitive_type : K_UINT64",
    "primitive_type : K_NATIVE K_INT",
    "primitive_type : K_NATIVE K_UNSIGNED K_INT",
    "primitive_type : K_NATIVE K_UINT",
    "primitive_type : K_TYPEDREF",
    "primitive_type : K_CHAR",
    "primitive_type : K_WCHAR",
    "primitive_type : K_VOID",
    "primitive_type : K_BOOL",
    "primitive_type : K_STRING",
    "bounds : bound",
    "bounds : bounds COMMA bound",
    "bound :",
    "bound : ELLIPSIS",
    "bound : int32",
    "bound : int32 ELLIPSIS int32",
    "bound : int32 ELLIPSIS",
    "call_conv : K_INSTANCE call_conv",
    "call_conv : K_EXPLICIT call_conv",
    "call_conv : call_kind",
    "call_kind :",
    "call_kind : K_DEFAULT",
    "call_kind : K_VARARG",
    "call_kind : K_UNMANAGED K_CDECL",
    "call_kind : K_UNMANAGED K_STDCALL",
    "call_kind : K_UNMANAGED K_THISCALL",
    "call_kind : K_UNMANAGED K_FASTCALL",
    "native_type :",
    "native_type : K_CUSTOM OPEN_PARENS comp_qstring COMMA comp_qstring CLOSE_PARENS",
    "native_type : K_FIXED K_SYSSTRING OPEN_BRACKET int32 CLOSE_BRACKET",
    "native_type : K_FIXED K_ARRAY OPEN_BRACKET int32 CLOSE_BRACKET",
    "native_type : K_VARIANT",
    "native_type : K_CURRENCY",
    "native_type : K_SYSCHAR",
    "native_type : K_VOID",
    "native_type : K_BOOL",
    "native_type : K_INT8",
    "native_type : K_INT16",
    "native_type : K_INT32",
    "native_type : K_INT64",
    "native_type : K_FLOAT32",
    "native_type : K_FLOAT64",
    "native_type : K_ERROR",
    "native_type : K_UNSIGNED K_INT8",
    "native_type : K_UINT8",
    "native_type : K_UNSIGNED K_INT16",
    "native_type : K_UINT16",
    "native_type : K_UNSIGNED K_INT32",
    "native_type : K_UINT32",
    "native_type : K_UNSIGNED K_INT64",
    "native_type : K_UINT64",
    "native_type : native_type STAR",
    "native_type : native_type OPEN_BRACKET CLOSE_BRACKET",
    "native_type : native_type OPEN_BRACKET int32 CLOSE_BRACKET",
    "native_type : native_type OPEN_BRACKET int32 PLUS int32 CLOSE_BRACKET",
    "native_type : native_type OPEN_BRACKET PLUS int32 CLOSE_BRACKET",
    "native_type : K_DECIMAL",
    "native_type : K_DATE",
    "native_type : K_BSTR",
    "native_type : K_LPSTR",
    "native_type : K_LPWSTR",
    "native_type : K_LPTSTR",
    "native_type : K_VBBYREFSTR",
    "native_type : K_OBJECTREF",
    "native_type : K_IUNKNOWN",
    "native_type : K_IDISPATCH",
    "native_type : K_STRUCT",
    "native_type : K_INTERFACE",
    "native_type : K_SAFEARRAY variant_type",
    "native_type : K_SAFEARRAY variant_type COMMA comp_qstring",
    "native_type : K_INT",
    "native_type : K_UNSIGNED K_INT",
    "native_type : K_NESTED K_STRUCT",
    "native_type : K_BYVALSTR",
    "native_type : K_ANSI K_BSTR",
    "native_type : K_TBSTR",
    "native_type : K_VARIANT K_BOOL",
    "native_type : K_METHOD",
    "native_type : K_AS K_ANY",
    "native_type : K_LPSTRUCT",
    "variant_type :",
    "variant_type : K_NULL",
    "variant_type : K_VARIANT",
    "variant_type : K_CURRENCY",
    "variant_type : K_VOID",
    "variant_type : K_BOOL",
    "variant_type : K_INT8",
    "variant_type : K_INT16",
    "variant_type : K_INT32",
    "variant_type : K_INT64",
    "variant_type : K_FLOAT32",
    "variant_type : K_FLOAT64",
    "variant_type : K_UNSIGNED K_INT8",
    "variant_type : K_UNSIGNED K_INT16",
    "variant_type : K_UNSIGNED K_INT32",
    "variant_type : K_UNSIGNED K_INT64",
    "variant_type : STAR",
    "variant_type : variant_type OPEN_BRACKET CLOSE_BRACKET",
    "variant_type : variant_type K_VECTOR",
    "variant_type : variant_type AMPERSAND",
    "variant_type : K_DECIMAL",
    "variant_type : K_DATE",
    "variant_type : K_BSTR",
    "variant_type : K_LPSTR",
    "variant_type : K_LPWSTR",
    "variant_type : K_IUNKNOWN",
    "variant_type : K_IDISPATCH",
    "variant_type : K_SAFEARRAY",
    "variant_type : K_INT",
    "variant_type : K_UNSIGNED K_INT",
    "variant_type : K_ERROR",
    "variant_type : K_HRESULT",
    "variant_type : K_CARRAY",
    "variant_type : K_USERDEFINED",
    "variant_type : K_RECORD",
    "variant_type : K_FILETIME",
    "variant_type : K_BLOB",
    "variant_type : K_STREAM",
    "variant_type : K_STORAGE",
    "variant_type : K_STREAMED_OBJECT",
    "variant_type : K_STORED_OBJECT",
    "variant_type : K_BLOB_OBJECT",
    "variant_type : K_CF",
    "variant_type : K_CLSID",
    "custom_modifier_type : primitive_type",
    "custom_modifier_type : class_ref",
    "field_decl : D_FIELD repeat_opt field_attr type id at_opt init_opt semicolon_opt",
    "repeat_opt :",
    "repeat_opt : OPEN_BRACKET int32 CLOSE_BRACKET",
    "field_attr :",
    "field_attr : field_attr K_PUBLIC",
    "field_attr : field_attr K_PRIVATE",
    "field_attr : field_attr K_FAMILY",
    "field_attr : field_attr K_ASSEMBLY",
    "field_attr : field_attr K_FAMANDASSEM",
    "field_attr : field_attr K_FAMORASSEM",
    "field_attr : field_attr K_PRIVATESCOPE",
    "field_attr : field_attr K_STATIC",
    "field_attr : field_attr K_INITONLY",
    "field_attr : field_attr K_RTSPECIALNAME",
    "field_attr : field_attr K_SPECIALNAME",
    "field_attr : field_attr K_MARSHAL OPEN_PARENS native_type CLOSE_PARENS",
    "field_attr : field_attr K_LITERAL",
    "field_attr : field_attr K_NOTSERIALIZED",
    "at_opt :",
    "at_opt : K_AT id",
    "init_opt :",
    "init_opt : ASSIGN field_init",
    "field_init_primitive : K_FLOAT32 OPEN_PARENS float64 CLOSE_PARENS",
    "field_init_primitive : K_FLOAT64 OPEN_PARENS float64 CLOSE_PARENS",
    "field_init_primitive : K_FLOAT32 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_FLOAT64 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_INT64 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_UINT64 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_INT32 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_UINT32 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_INT16 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_UINT16 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_CHAR OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_WCHAR OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_INT8 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_UINT8 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_BOOL OPEN_PARENS truefalse CLOSE_PARENS",
    "field_init_full : field_init_primitive",
    "field_init_full : K_BYTEARRAY bytes_list",
    "field_init : field_init_full",
    "field_init : comp_qstring",
    "field_init : K_NULLREF",
    "member_init : field_init_full",
    "member_init : K_STRING OPEN_PARENS SQSTRING CLOSE_PARENS",
    "opt_truefalse_list : truefalse_list",
    "truefalse_list : truefalse",
    "truefalse_list : truefalse_list truefalse",
    "data_decl : data_head data_body",
    "data_head : D_DATA data_attr id ASSIGN",
    "data_head : D_DATA data_attr",
    "data_attr :",
    "data_attr : K_TLS",
    "data_attr : K_CIL",
    "data_body : OPEN_BRACE dataitem_list CLOSE_BRACE",
    "data_body : dataitem",
    "dataitem_list : dataitem",
    "dataitem_list : dataitem_list COMMA dataitem",
    "dataitem : K_CHAR STAR OPEN_PARENS comp_qstring CLOSE_PARENS",
    "dataitem : K_WCHAR STAR OPEN_PARENS comp_qstring CLOSE_PARENS",
    "dataitem : AMPERSAND OPEN_PARENS id CLOSE_PARENS",
    "dataitem : K_BYTEARRAY ASSIGN bytes_list",
    "dataitem : K_BYTEARRAY bytes_list",
    "dataitem : K_FLOAT32 OPEN_PARENS float64 CLOSE_PARENS repeat_opt",
    "dataitem : K_FLOAT64 OPEN_PARENS float64 CLOSE_PARENS repeat_opt",
    "dataitem : K_INT64 OPEN_PARENS int64 CLOSE_PARENS repeat_opt",
    "dataitem : K_INT32 OPEN_PARENS int32 CLOSE_PARENS repeat_opt",
    "dataitem : K_INT16 OPEN_PARENS int32 CLOSE_PARENS repeat_opt",
    "dataitem : K_INT8 OPEN_PARENS int32 CLOSE_PARENS repeat_opt",
    "dataitem : K_FLOAT32 repeat_opt",
    "dataitem : K_FLOAT64 repeat_opt",
    "dataitem : K_INT64 repeat_opt",
    "dataitem : K_INT32 repeat_opt",
    "dataitem : K_INT16 repeat_opt",
    "dataitem : K_INT8 repeat_opt",
    "method_all : method_head OPEN_BRACE method_decls CLOSE_BRACE",
    "method_head : D_METHOD meth_attr call_conv param_attr type method_name formal_typars_clause OPEN_PARENS sig_args CLOSE_PARENS impl_attr",
    "method_head : D_METHOD meth_attr call_conv param_attr type K_MARSHAL OPEN_PARENS native_type CLOSE_PARENS method_name OPEN_PARENS sig_args CLOSE_PARENS impl_attr",
    "meth_attr :",
    "meth_attr : meth_attr K_STATIC",
    "meth_attr : meth_attr K_PUBLIC",
    "meth_attr : meth_attr K_PRIVATE",
    "meth_attr : meth_attr K_FAMILY",
    "meth_attr : meth_attr K_ASSEMBLY",
    "meth_attr : meth_attr K_FAMANDASSEM",
    "meth_attr : meth_attr K_FAMORASSEM",
    "meth_attr : meth_attr K_PRIVATESCOPE",
    "meth_attr : meth_attr K_FINAL",
    "meth_attr : meth_attr K_VIRTUAL",
    "meth_attr : meth_attr K_ABSTRACT",
    "meth_attr : meth_attr K_HIDEBYSIG",
    "meth_attr : meth_attr K_NEWSLOT",
    "meth_attr : meth_attr K_REQSECOBJ",
    "meth_attr : meth_attr K_SPECIALNAME",
    "meth_attr : meth_attr K_RTSPECIALNAME",
    "meth_attr : meth_attr K_STRICT",
    "meth_attr : meth_attr K_COMPILERCONTROLLED",
    "meth_attr : meth_attr K_UNMANAGEDEXP",
    "meth_attr : meth_attr K_PINVOKEIMPL OPEN_PARENS comp_qstring K_AS comp_qstring pinv_attr CLOSE_PARENS",
    "meth_attr : meth_attr K_PINVOKEIMPL OPEN_PARENS comp_qstring pinv_attr CLOSE_PARENS",
    "meth_attr : meth_attr K_PINVOKEIMPL OPEN_PARENS pinv_attr CLOSE_PARENS",
    "pinv_attr :",
    "pinv_attr : pinv_attr K_NOMANGLE",
    "pinv_attr : pinv_attr K_ANSI",
    "pinv_attr : pinv_attr K_UNICODE",
    "pinv_attr : pinv_attr K_AUTOCHAR",
    "pinv_attr : pinv_attr K_LASTERR",
    "pinv_attr : pinv_attr K_WINAPI",
    "pinv_attr : pinv_attr K_CDECL",
    "pinv_attr : pinv_attr K_STDCALL",
    "pinv_attr : pinv_attr K_THISCALL",
    "pinv_attr : pinv_attr K_FASTCALL",
    "pinv_attr : pinv_attr K_BESTFIT COLON K_ON",
    "pinv_attr : pinv_attr K_BESTFIT COLON K_OFF",
    "pinv_attr : pinv_attr K_CHARMAPERROR COLON K_ON",
    "pinv_attr : pinv_attr K_CHARMAPERROR COLON K_OFF",
    "method_name : D_CTOR",
    "method_name : D_CCTOR",
    "method_name : comp_name",
    "param_attr :",
    "param_attr : param_attr OPEN_BRACKET K_IN CLOSE_BRACKET",
    "param_attr : param_attr OPEN_BRACKET K_OUT CLOSE_BRACKET",
    "param_attr : param_attr OPEN_BRACKET K_OPT CLOSE_BRACKET",
    "impl_attr :",
    "impl_attr : impl_attr K_NATIVE",
    "impl_attr : impl_attr K_CIL",
    "impl_attr : impl_attr K_IL",
    "impl_attr : impl_attr K_OPTIL",
    "impl_attr : impl_attr K_MANAGED",
    "impl_attr : impl_attr K_UNMANAGED",
    "impl_attr : impl_attr K_FORWARDREF",
    "impl_attr : impl_attr K_PRESERVESIG",
    "impl_attr : impl_attr K_RUNTIME",
    "impl_attr : impl_attr K_INTERNALCALL",
    "impl_attr : impl_attr K_SYNCHRONIZED",
    "impl_attr : impl_attr K_NOINLINING",
    "impl_attr : impl_attr K_NOOPTIMIZATION",
    "impl_attr : impl_attr K_AGGRESSIVEINLINING",
    "sig_args :",
    "sig_args : sig_arg_list",
    "sig_arg_list : sig_arg",
    "sig_arg_list : sig_arg_list COMMA sig_arg",
    "sig_arg : param_attr type",
    "sig_arg : param_attr type id",
    "sig_arg : ELLIPSIS",
    "sig_arg : param_attr type K_MARSHAL OPEN_PARENS native_type CLOSE_PARENS",
    "sig_arg : param_attr type K_MARSHAL OPEN_PARENS native_type CLOSE_PARENS id",
    "type_list :",
    "type_list : ELLIPSIS",
    "type_list : type_list COMMA ELLIPSIS",
    "type_list : param_attr type opt_id",
    "type_list : type_list COMMA param_attr type opt_id",
    "opt_id :",
    "opt_id : id",
    "method_decls :",
    "method_decls : method_decls method_decl",
    "method_decl : D_EMITBYTE int32",
    "method_decl : D_MAXSTACK int32",
    "method_decl : D_LOCALS OPEN_PARENS local_list CLOSE_PARENS",
    "method_decl : D_LOCALS K_INIT OPEN_PARENS local_list CLOSE_PARENS",
    "method_decl : D_ENTRYPOINT",
    "method_decl : D_ZEROINIT",
    "method_decl : D_EXPORT OPEN_BRACKET int32 CLOSE_BRACKET",
    "method_decl : D_EXPORT OPEN_BRACKET int32 CLOSE_BRACKET K_AS id",
    "method_decl : D_VTENTRY int32 COLON int32",
    "method_decl : D_OVERRIDE type_spec DOUBLE_COLON method_name",
    "method_decl : D_OVERRIDE K_METHOD method_ref",
    "method_decl : D_OVERRIDE K_METHOD call_conv type type_spec DOUBLE_COLON method_name OPEN_ANGLE_BRACKET OPEN_BRACKET int32 CLOSE_BRACKET CLOSE_ANGLE_BRACKET OPEN_PARENS type_list CLOSE_PARENS",
    "method_decl : scope_block",
    "method_decl : D_PARAM OPEN_BRACKET int32 CLOSE_BRACKET init_opt",
    "method_decl : param_type_decl",
    "method_decl : id COLON",
    "method_decl : seh_block",
    "method_decl : instr",
    "method_decl : sec_decl",
    "method_decl : extsource_spec",
    "method_decl : language_decl",
    "method_decl : customattr_decl",
    "method_decl : data_decl",
    "local_list :",
    "local_list : local",
    "local_list : local_list COMMA local",
    "local : type",
    "local : type id",
    "local : slot_num type",
    "local : slot_num type id",
    "slot_num : OPEN_BRACKET int32 CLOSE_BRACKET",
    "type_spec : OPEN_BRACKET slashed_name CLOSE_BRACKET",
    "type_spec : OPEN_BRACKET D_MODULE slashed_name CLOSE_BRACKET",
    "type_spec : type",
    "scope_block : scope_block_begin method_decls CLOSE_BRACE",
    "scope_block_begin : OPEN_BRACE",
    "seh_block : try_block seh_clauses",
    "try_block : D_TRY scope_block",
    "try_block : D_TRY id K_TO id",
    "try_block : D_TRY int32 K_TO int32",
    "seh_clauses : seh_clause",
    "seh_clauses : seh_clauses seh_clause",
    "seh_clause : K_CATCH type handler_block",
    "seh_clause : K_FINALLY handler_block",
    "seh_clause : K_FAULT handler_block",
    "seh_clause : filter_clause handler_block",
    "filter_clause : K_FILTER scope_block",
    "filter_clause : K_FILTER id",
    "filter_clause : K_FILTER int32",
    "handler_block : scope_block",
    "handler_block : K_HANDLER id K_TO id",
    "handler_block : K_HANDLER int32 K_TO int32",
    "instr : INSTR_NONE",
    "instr : INSTR_LOCAL int32",
    "instr : INSTR_LOCAL id",
    "instr : INSTR_PARAM int32",
    "instr : INSTR_PARAM id",
    "instr : INSTR_I int32",
    "instr : INSTR_I id",
    "instr : INSTR_I8 int64",
    "instr : INSTR_R float64",
    "instr : INSTR_R int64",
    "instr : INSTR_R bytes_list",
    "instr : INSTR_BRTARGET int32",
    "instr : INSTR_BRTARGET id",
    "instr : INSTR_METHOD method_ref",
    "instr : INSTR_FIELD type type_spec DOUBLE_COLON id",
    "instr : INSTR_FIELD type id",
    "instr : INSTR_TYPE type_spec",
    "instr : INSTR_STRING comp_qstring",
    "instr : INSTR_STRING K_BYTEARRAY ASSIGN bytes_list",
    "instr : INSTR_STRING K_BYTEARRAY bytes_list",
    "instr : INSTR_SIG call_conv type OPEN_PARENS type_list CLOSE_PARENS",
    "instr : INSTR_TOK owner_type",
    "instr : INSTR_SWITCH OPEN_PARENS labels CLOSE_PARENS",
    "method_ref : call_conv type method_name typars_clause OPEN_PARENS type_list CLOSE_PARENS",
    "method_ref : call_conv type type_spec DOUBLE_COLON method_name typars_clause OPEN_PARENS type_list CLOSE_PARENS",
    "labels :",
    "labels : id",
    "labels : int32",
    "labels : labels COMMA id",
    "labels : labels COMMA int32",
    "owner_type : type_spec",
    "owner_type : member_ref",
    "member_ref : K_METHOD method_ref",
    "member_ref : K_FIELD type type_spec DOUBLE_COLON id",
    "member_ref : K_FIELD type id",
    "event_all : event_head OPEN_BRACE event_decls CLOSE_BRACE",
    "event_head : D_EVENT event_attr type_spec comp_name",
    "event_head : D_EVENT event_attr id",
    "event_attr :",
    "event_attr : event_attr K_RTSPECIALNAME",
    "event_attr : event_attr K_SPECIALNAME",
    "event_decls :",
    "event_decls : event_decls event_decl",
    "event_decl : D_ADDON method_ref semicolon_opt",
    "event_decl : D_REMOVEON method_ref semicolon_opt",
    "event_decl : D_FIRE method_ref semicolon_opt",
    "event_decl : D_OTHER method_ref semicolon_opt",
    "event_decl : customattr_decl",
    "event_decl : extsource_spec",
    "event_decl : language_decl",
    "prop_all : prop_head OPEN_BRACE prop_decls CLOSE_BRACE",
    "prop_head : D_PROPERTY prop_attr type comp_name OPEN_PARENS type_list CLOSE_PARENS init_opt",
    "prop_attr :",
    "prop_attr : prop_attr K_RTSPECIALNAME",
    "prop_attr : prop_attr K_SPECIALNAME",
    "prop_attr : prop_attr K_INSTANCE",
    "prop_decls :",
    "prop_decls : prop_decls prop_decl",
    "prop_decl : D_SET method_ref",
    "prop_decl : D_GET method_ref",
    "prop_decl : D_OTHER method_ref",
    "prop_decl : customattr_decl",
    "prop_decl : extsource_spec",
    "prop_decl : language_decl",
    "customattr_decl : D_CUSTOM customattr_owner_type_opt custom_type",
    "customattr_decl : D_CUSTOM customattr_owner_type_opt custom_type ASSIGN comp_qstring",
    "customattr_decl : D_CUSTOM customattr_owner_type_opt custom_type ASSIGN bytes_list",
    "customattr_decl : D_CUSTOM customattr_owner_type_opt custom_type ASSIGN OPEN_BRACE customattr_values CLOSE_BRACE",
    "customattr_owner_type_opt :",
    "customattr_owner_type_opt : OPEN_PARENS type CLOSE_PARENS",
    "customattr_values :",
    "customattr_values : K_BOOL OPEN_BRACKET int32 CLOSE_BRACKET OPEN_PARENS opt_truefalse_list CLOSE_PARENS",
    "customattr_values : K_BYTEARRAY bytes_list",
    "customattr_values : K_STRING OPEN_PARENS SQSTRING CLOSE_PARENS",
    "customattr_values : customattr_ctor_args",
    "customattr_ctor_args : customattr_ctor_arg",
    "customattr_ctor_args : customattr_ctor_args customattr_ctor_arg",
    "customattr_ctor_arg : field_init_primitive",
    "customattr_ctor_arg : K_TYPE OPEN_PARENS type CLOSE_PARENS",
    "custom_type : call_conv type type_spec DOUBLE_COLON method_name OPEN_PARENS type_list CLOSE_PARENS",
    "custom_type : call_conv type method_name OPEN_PARENS type_list CLOSE_PARENS",
    "sec_decl : D_PERMISSION sec_action type_spec OPEN_PARENS nameval_pairs CLOSE_PARENS",
    "sec_decl : D_PERMISSION sec_action type_spec",
    "sec_decl : D_PERMISSIONSET sec_action ASSIGN bytes_list",
    "sec_decl : D_PERMISSIONSET sec_action comp_qstring",
    "sec_decl : D_PERMISSIONSET sec_action ASSIGN OPEN_BRACE permissions CLOSE_BRACE",
    "permissions : permission",
    "permissions : permissions COMMA permission",
    "permission : class_ref ASSIGN OPEN_BRACE permission_members CLOSE_BRACE",
    "permission_members : permission_member",
    "permission_members : permission_members permission_member",
    "permission_member : prop_or_field primitive_type perm_mbr_nameval_pair",
    "permission_member : prop_or_field K_ENUM class_ref perm_mbr_nameval_pair",
    "perm_mbr_nameval_pair : SQSTRING ASSIGN member_init",
    "prop_or_field : K_PROPERTY",
    "prop_or_field : K_FIELD",
    "nameval_pairs : nameval_pair",
    "nameval_pairs : nameval_pairs COMMA nameval_pair",
    "nameval_pair : comp_qstring ASSIGN cavalue",
    "cavalue : truefalse",
    "cavalue : int32",
    "cavalue : int32 OPEN_PARENS int32 CLOSE_PARENS",
    "cavalue : comp_qstring",
    "cavalue : class_ref OPEN_PARENS K_INT8 COLON int32 CLOSE_PARENS",
    "cavalue : class_ref OPEN_PARENS K_INT16 COLON int32 CLOSE_PARENS",
    "cavalue : class_ref OPEN_PARENS K_INT32 COLON int32 CLOSE_PARENS",
    "cavalue : class_ref OPEN_PARENS int32 CLOSE_PARENS",
    "sec_action : K_REQUEST",
    "sec_action : K_DEMAND",
    "sec_action : K_ASSERT",
    "sec_action : K_DENY",
    "sec_action : K_PERMITONLY",
    "sec_action : K_LINKCHECK",
    "sec_action : K_INHERITCHECK",
    "sec_action : K_REQMIN",
    "sec_action : K_REQOPT",
    "sec_action : K_REQREFUSE",
    "sec_action : K_PREJITGRANT",
    "sec_action : K_PREJITDENY",
    "sec_action : K_NONCASDEMAND",
    "sec_action : K_NONCASLINKDEMAND",
    "sec_action : K_NONCASINHERITANCE",
    "module_head : D_MODULE",
    "module_head : D_MODULE comp_name",
    "module_head : D_MODULE K_EXTERN comp_name",
    "file_decl : D_FILE file_attr comp_name file_entry D_HASH ASSIGN bytes_list file_entry",
    "file_decl : D_FILE file_attr comp_name file_entry",
    "file_attr :",
    "file_attr : file_attr K_NOMETADATA",
    "file_entry :",
    "file_entry : D_ENTRYPOINT",
    "assembly_all : assembly_head OPEN_BRACE assembly_decls CLOSE_BRACE",
    "assembly_head : D_ASSEMBLY legacylibrary_opt asm_attr slashed_name",
    "asm_attr :",
    "asm_attr : asm_attr K_RETARGETABLE",
    "assembly_decls :",
    "assembly_decls : assembly_decls assembly_decl",
    "assembly_decl : D_PUBLICKEY ASSIGN bytes_list",
    "assembly_decl : D_VER int32 COLON int32 COLON int32 COLON int32",
    "assembly_decl : D_LOCALE comp_qstring",
    "assembly_decl : D_LOCALE ASSIGN bytes_list",
    "assembly_decl : D_HASH K_ALGORITHM int32",
    "assembly_decl : customattr_decl",
    "assembly_decl : sec_decl",
    "asm_or_ref_decl : D_PUBLICKEY ASSIGN bytes_list",
    "asm_or_ref_decl : D_VER int32 COLON int32 COLON int32 COLON int32",
    "asm_or_ref_decl : D_LOCALE comp_qstring",
    "asm_or_ref_decl : D_LOCALE ASSIGN bytes_list",
    "asm_or_ref_decl : customattr_decl",
    "assemblyref_all : assemblyref_head OPEN_BRACE assemblyref_decls CLOSE_BRACE",
    "assemblyref_head : D_ASSEMBLY K_EXTERN legacylibrary_opt asm_attr slashed_name",
    "assemblyref_head : D_ASSEMBLY K_EXTERN legacylibrary_opt asm_attr slashed_name K_AS slashed_name",
    "assemblyref_decls :",
    "assemblyref_decls : assemblyref_decls assemblyref_decl",
    "assemblyref_decl : D_VER int32 COLON int32 COLON int32 COLON int32",
    "assemblyref_decl : D_PUBLICKEY ASSIGN bytes_list",
    "assemblyref_decl : D_PUBLICKEYTOKEN ASSIGN bytes_list",
    "assemblyref_decl : D_LOCALE comp_qstring",
    "assemblyref_decl : D_LOCALE ASSIGN bytes_list",
    "assemblyref_decl : D_HASH ASSIGN bytes_list",
    "assemblyref_decl : customattr_decl",
    "assemblyref_decl : K_AUTO",
    "exptype_all : exptype_head OPEN_BRACE exptype_decls CLOSE_BRACE",
    "exptype_head : D_CLASS K_EXTERN expt_attr comp_name",
    "expt_attr :",
    "expt_attr : expt_attr K_PRIVATE",
    "expt_attr : expt_attr K_PUBLIC",
    "expt_attr : expt_attr K_FORWARDER",
    "expt_attr : expt_attr K_NESTED K_PUBLIC",
    "expt_attr : expt_attr K_NESTED K_PRIVATE",
    "expt_attr : expt_attr K_NESTED K_FAMILY",
    "expt_attr : expt_attr K_NESTED K_ASSEMBLY",
    "expt_attr : expt_attr K_NESTED K_FAMANDASSEM",
    "expt_attr : expt_attr K_NESTED K_FAMORASSEM",
    "exptype_decls :",
    "exptype_decls : exptype_decls exptype_decl",
    "exptype_decl : D_FILE comp_name",
    "exptype_decl : D_CLASS K_EXTERN comp_name",
    "exptype_decl : customattr_decl",
    "exptype_decl : D_ASSEMBLY K_EXTERN comp_name",
    "manifestres_all : manifestres_head OPEN_BRACE manifestres_decls CLOSE_BRACE",
    "manifestres_head : D_MRESOURCE manres_attr comp_name",
    "manres_attr :",
    "manres_attr : manres_attr K_PUBLIC",
    "manres_attr : manres_attr K_PRIVATE",
    "manifestres_decls :",
    "manifestres_decls : manifestres_decls manifestres_decl",
    "manifestres_decl : D_FILE comp_name K_AT int32",
    "manifestres_decl : D_ASSEMBLY K_EXTERN slashed_name",
    "manifestres_decl : customattr_decl",
    "comp_qstring : QSTRING",
    "comp_qstring : comp_qstring PLUS QSTRING",
    "int32 : INT64",
    "int64 : INT64",
    "float64 : FLOAT64",
    "float64 : K_FLOAT32 OPEN_PARENS INT32 CLOSE_PARENS",
    "float64 : K_FLOAT32 OPEN_PARENS INT64 CLOSE_PARENS",
    "float64 : K_FLOAT64 OPEN_PARENS INT64 CLOSE_PARENS",
    "float64 : K_FLOAT64 OPEN_PARENS INT32 CLOSE_PARENS",
    "hexbyte : HEXBYTE",
    "$$2 :",
    "bytes_list : OPEN_PARENS $$2 bytes CLOSE_PARENS",
    "bytes :",
    "bytes : hexbytes",
    "hexbytes : hexbyte",
    "hexbytes : hexbytes hexbyte",
    "truefalse : K_TRUE",
    "truefalse : K_FALSE",
    "id : ID",
    "id : SQSTRING",
    "comp_name : id",
    "comp_name : comp_name DOT comp_name",
    "comp_name : COMP_NAME",
    "semicolon_opt :",
    "semicolon_opt : SEMICOLON",
    "legacylibrary_opt :",
    "legacylibrary_opt : K_LEGACY K_LIBRARY",
  };
            public static string getRule(int index)
            {
                return yyRule[index];
            }
        }
        protected static readonly string[] yyNames = {
    "end-of-file",null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,"'!'",null,null,null,null,"'&'",
    null,"'('","')'","'*'","'+'","','","'-'","'.'","'/'",null,null,null,
    null,null,null,null,null,null,null,"':'","';'","'<'","'='","'>'",null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,
    "'['",null,"']'",null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,"'{'",null,"'}'",null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    "EOF","ID","QSTRING","SQSTRING","COMP_NAME","INT32","INT64","FLOAT64",
    "HEXBYTE","DOT","OPEN_BRACE","CLOSE_BRACE","OPEN_BRACKET",
    "CLOSE_BRACKET","OPEN_PARENS","CLOSE_PARENS","COMMA","COLON",
    "DOUBLE_COLON","\"::\"","SEMICOLON","ASSIGN","STAR","AMPERSAND",
    "PLUS","SLASH","BANG","ELLIPSIS","\"...\"","DASH",
    "OPEN_ANGLE_BRACKET","CLOSE_ANGLE_BRACKET","UNKNOWN","INSTR_NONE",
    "INSTR_VAR","INSTR_I","INSTR_I8","INSTR_R","INSTR_BRTARGET",
    "INSTR_METHOD","INSTR_NEWOBJ","INSTR_FIELD","INSTR_TYPE",
    "INSTR_STRING","INSTR_SIG","INSTR_RVA","INSTR_TOK","INSTR_SWITCH",
    "INSTR_PHI","INSTR_LOCAL","INSTR_PARAM","D_ADDON","D_ALGORITHM",
    "D_ASSEMBLY","D_BACKING","D_BLOB","D_CAPABILITY","D_CCTOR","D_CLASS",
    "D_COMTYPE","D_CONFIG","D_IMAGEBASE","D_CORFLAGS","D_CTOR","D_CUSTOM",
    "D_DATA","D_EMITBYTE","D_ENTRYPOINT","D_EVENT","D_EXELOC","D_EXPORT",
    "D_FIELD","D_FILE","D_FIRE","D_GET","D_HASH","D_IMPLICITCOM",
    "D_LANGUAGE","D_LINE","D_XLINE","D_LOCALE","D_LOCALS","D_MANIFESTRES",
    "D_MAXSTACK","D_METHOD","D_MIME","D_MODULE","D_MRESOURCE",
    "D_NAMESPACE","D_ORIGINATOR","D_OS","D_OTHER","D_OVERRIDE","D_PACK",
    "D_PARAM","D_PERMISSION","D_PERMISSIONSET","D_PROCESSOR","D_PROPERTY",
    "D_PUBLICKEY","D_PUBLICKEYTOKEN","D_REMOVEON","D_SET","D_SIZE",
    "D_STACKRESERVE","D_SUBSYSTEM","D_TITLE","D_TRY","D_VER","D_VTABLE",
    "D_VTENTRY","D_VTFIXUP","D_ZEROINIT","K_AT","K_AS",
    "K_AGGRESSIVEINLINING","K_IMPLICITCOM","K_IMPLICITRES",
    "K_NOAPPDOMAIN","K_NOPROCESS","K_NOMACHINE","K_EXTERN","K_INSTANCE",
    "K_EXPLICIT","K_DEFAULT","K_VARARG","K_UNMANAGED","K_CDECL",
    "K_STDCALL","K_THISCALL","K_FASTCALL","K_MARSHAL","K_IN","K_OUT",
    "K_OPT","K_STATIC","K_PUBLIC","K_PRIVATE","K_FAMILY","K_INITONLY",
    "K_RTSPECIALNAME","K_STRICT","K_SPECIALNAME","K_ASSEMBLY",
    "K_FAMANDASSEM","K_FAMORASSEM","K_PRIVATESCOPE","K_LITERAL",
    "K_NOTSERIALIZED","K_VALUE","K_NOT_IN_GC_HEAP","K_INTERFACE",
    "K_SEALED","K_ABSTRACT","K_AUTO","K_SEQUENTIAL","K_ANSI","K_UNICODE",
    "K_AUTOCHAR","K_BESTFIT","K_IMPORT","K_SERIALIZABLE","K_NESTED",
    "K_LATEINIT","K_EXTENDS","K_IMPLEMENTS","K_FINAL","K_VIRTUAL",
    "K_HIDEBYSIG","K_NEWSLOT","K_UNMANAGEDEXP","K_PINVOKEIMPL",
    "K_NOMANGLE","K_OLE","K_LASTERR","K_WINAPI","K_NATIVE","K_IL","K_CIL",
    "K_OPTIL","K_MANAGED","K_FORWARDREF","K_RUNTIME","K_INTERNALCALL",
    "K_SYNCHRONIZED","K_NOINLINING","K_NOOPTIMIZATION","K_CUSTOM",
    "K_FIXED","K_SYSSTRING","K_ARRAY","K_VARIANT","K_CURRENCY",
    "K_SYSCHAR","K_VOID","K_BOOL","K_INT8","K_INT16","K_INT32","K_INT64",
    "K_FLOAT32","K_FLOAT64","K_ERROR","K_UNSIGNED","K_UINT","K_UINT8",
    "K_UINT16","K_UINT32","K_UINT64","K_DECIMAL","K_DATE","K_BSTR",
    "K_LPSTR","K_LPWSTR","K_LPTSTR","K_VBBYREFSTR","K_OBJECTREF",
    "K_IUNKNOWN","K_IDISPATCH","K_STRUCT","K_SAFEARRAY","K_INT",
    "K_BYVALSTR","K_TBSTR","K_LPVOID","K_ANY","K_FLOAT","K_LPSTRUCT",
    "K_NULL","K_PTR","K_VECTOR","K_HRESULT","K_CARRAY","K_USERDEFINED",
    "K_RECORD","K_FILETIME","K_BLOB","K_STREAM","K_STORAGE",
    "K_STREAMED_OBJECT","K_STORED_OBJECT","K_BLOB_OBJECT","K_CF",
    "K_CLSID","K_METHOD","K_CLASS","K_PINNED","K_MODREQ","K_MODOPT",
    "K_TYPEDREF","K_TYPE","K_WCHAR","K_CHAR","K_FROMUNMANAGED",
    "K_CALLMOSTDERIVED","K_BYTEARRAY","K_WITH","K_INIT","K_TO","K_CATCH",
    "K_FILTER","K_FINALLY","K_FAULT","K_HANDLER","K_TLS","K_FIELD",
    "K_PROPERTY","K_REQUEST","K_DEMAND","K_ASSERT","K_DENY",
    "K_PERMITONLY","K_LINKCHECK","K_INHERITCHECK","K_REQMIN","K_REQOPT",
    "K_REQREFUSE","K_PREJITGRANT","K_PREJITDENY","K_NONCASDEMAND",
    "K_NONCASLINKDEMAND","K_NONCASINHERITANCE","K_READONLY",
    "K_NOMETADATA","K_ALGORITHM","K_FULLORIGIN","K_ENABLEJITTRACKING",
    "K_DISABLEJITOPTIMIZER","K_RETARGETABLE","K_PRESERVESIG",
    "K_BEFOREFIELDINIT","K_ALIGNMENT","K_NULLREF","K_VALUETYPE",
    "K_COMPILERCONTROLLED","K_REQSECOBJ","K_ENUM","K_OBJECT","K_STRING",
    "K_TRUE","K_FALSE","K_IS","K_ON","K_OFF","K_FORWARDER",
    "K_CHARMAPERROR","K_LEGACY","K_LIBRARY",
  };

        /** index-checked interface to yyNames[].
            @param token single character or %token value.
            @return token name or [illegal] or [unknown].
          */
        public static string yyname(int token)
        {
            if ((token < 0) || (token > yyNames.Length)) return "[illegal]";
            string name;
            if ((name = yyNames[token]) != null) return name;
            return "[unknown]";
        }

#pragma warning disable 414
        int yyExpectingState;
#pragma warning restore 414
        /** computes list of expected tokens on error by tracing the tables.
            @param state for which to compute the list.
            @return list of token names.
          */
        protected int[] yyExpectingTokens(int state)
        {
            int token, n, len = 0;
            bool[] ok = new bool[yyNames.Length];
            if ((n = yySindex[state]) != 0)
                for (token = n < 0 ? -n : 0;
                     (token < yyNames.Length) && (n + token < yyTable.Length); ++token)
                    if (yyCheck[n + token] == token && !ok[token] && yyNames[token] != null)
                    {
                        ++len;
                        ok[token] = true;
                    }
            if ((n = yyRindex[state]) != 0)
                for (token = n < 0 ? -n : 0;
                     (token < yyNames.Length) && (n + token < yyTable.Length); ++token)
                    if (yyCheck[n + token] == token && !ok[token] && yyNames[token] != null)
                    {
                        ++len;
                        ok[token] = true;
                    }
            int[] result = new int[len];
            for (n = token = 0; n < len; ++token)
                if (ok[token]) result[n++] = token;
            return result;
        }
        protected string[] yyExpecting(int state)
        {
            int[] tokens = yyExpectingTokens(state);
            string[] result = new string[tokens.Length];
            for (int n = 0; n < tokens.Length; n++)
                result[n] = yyNames[tokens[n]];
            return result;
        }

        /** the generated parser, with debugging messages.
            Maintains a state and a value stack, currently with fixed maximum size.
            @param yyLex scanner.
            @param yydebug debug message writer implementing yyDebug, or null.
            @return result of the last reduction, if any.
            @throws yyException on irrecoverable parse error.
          */
        internal Object yyparse(yyParser.yyInput yyLex, Object yyd)
        {
            this.debug = (yydebug.yyDebug)yyd;
            return yyparse(yyLex);
        }

        /** initial size and increment of the state/value stack [default 256].
            This is not final so that it can be overwritten outside of invocations
            of yyparse().
          */
        protected int yyMax;

        /** executed at the beginning of a reduce action.
            Used as $$ = yyDefault($1), prior to the user-specified action, if any.
            Can be overwritten to provide deep copy, etc.
            @param first value for $1, or null.
            @return first.
          */
        protected Object yyDefault(Object first)
        {
            return first;
        }

        static int[] global_yyStates;
        static object[] global_yyVals;
#pragma warning disable 649
        protected bool use_global_stacks;
#pragma warning restore 649
        object[] yyVals;                    // value stack
        object yyVal;                       // value stack ptr
        int yyToken;                        // current input
        int yyTop;

        /** the generated parser.
            Maintains a state and a value stack, currently with fixed maximum size.
            @param yyLex scanner.
            @return result of the last reduction, if any.
            @throws yyException on irrecoverable parse error.
          */
        internal Object yyparse(yyParser.yyInput yyLex)
        {
            if (yyMax <= 0) yyMax = 256;        // initial size
            int yyState = 0;                   // state stack ptr
            int[] yyStates;                 // state stack 
            yyVal = null;
            yyToken = -1;
            int yyErrorFlag = 0;                // #tks to shift
            if (use_global_stacks && global_yyStates != null)
            {
                yyVals = global_yyVals;
                yyStates = global_yyStates;
            }
            else
            {
                yyVals = new object[yyMax];
                yyStates = new int[yyMax];
                if (use_global_stacks)
                {
                    global_yyVals = yyVals;
                    global_yyStates = yyStates;
                }
            }

            /*yyLoop:*/
            for (yyTop = 0; ; ++yyTop)
            {
                if (yyTop >= yyStates.Length)
                {           // dynamically increase
                    global::System.Array.Resize(ref yyStates, yyStates.Length + yyMax);
                    global::System.Array.Resize(ref yyVals, yyVals.Length + yyMax);
                }
                yyStates[yyTop] = yyState;
                yyVals[yyTop] = yyVal;
                if (debug != null) debug.push(yyState, yyVal);

                /*yyDiscarded:*/
                while (true)
                {   // discarding a token does not change stack
                    int yyN;
                    if ((yyN = yyDefRed[yyState]) == 0)
                    {   // else [default] reduce (yyN)
                        if (yyToken < 0)
                        {
                            yyToken = yyLex.advance() ? yyLex.token() : 0;
                            if (debug != null)
                                debug.lex(yyState, yyToken, yyname(yyToken), yyLex.value());
                        }
                        if ((yyN = yySindex[yyState]) != 0 && ((yyN += yyToken) >= 0)
                            && (yyN < yyTable.Length) && (yyCheck[yyN] == yyToken))
                        {
                            if (debug != null)
                                debug.shift(yyState, yyTable[yyN], yyErrorFlag - 1);
                            yyState = yyTable[yyN];     // shift to yyN
                            yyVal = yyLex.value();
                            yyToken = -1;
                            if (yyErrorFlag > 0) --yyErrorFlag;
                            goto continue_yyLoop;
                        }
                        if ((yyN = yyRindex[yyState]) != 0 && (yyN += yyToken) >= 0
                            && yyN < yyTable.Length && yyCheck[yyN] == yyToken)
                            yyN = yyTable[yyN];         // reduce (yyN)
                        else
                            switch (yyErrorFlag)
                            {

                                case 0:
                                    yyExpectingState = yyState;
                                    // yyerror(String.Format ("syntax error, got token `{0}'", yyname (yyToken)), yyExpecting(yyState));
                                    if (debug != null) debug.error("syntax error");
                                    if (yyToken == 0 /*eof*/ || yyToken == eof_token) throw new yyParser.yyUnexpectedEof();
                                    goto case 1;
                                case 1:
                                case 2:
                                    yyErrorFlag = 3;
                                    do
                                    {
                                        if ((yyN = yySindex[yyStates[yyTop]]) != 0
                                            && (yyN += Token.yyErrorCode) >= 0 && yyN < yyTable.Length
                                            && yyCheck[yyN] == Token.yyErrorCode)
                                        {
                                            if (debug != null)
                                                debug.shift(yyStates[yyTop], yyTable[yyN], 3);
                                            yyState = yyTable[yyN];
                                            yyVal = yyLex.value();
                                            goto continue_yyLoop;
                                        }
                                        if (debug != null) debug.pop(yyStates[yyTop]);
                                    } while (--yyTop >= 0);
                                    if (debug != null) debug.reject();
                                    throw new yyParser.yyException("irrecoverable syntax error");

                                case 3:
                                    if (yyToken == 0)
                                    {
                                        if (debug != null) debug.reject();
                                        throw new yyParser.yyException("irrecoverable syntax error at end-of-file");
                                    }
                                    if (debug != null)
                                        debug.discard(yyState, yyToken, yyname(yyToken),
                                                      yyLex.value());
                                    yyToken = -1;
                                    goto continue_yyDiscarded;      // leave stack alone
                            }
                    }
                    int yyV = yyTop + 1 - yyLen[yyN];
                    if (debug != null)
                        debug.reduce(yyState, yyStates[yyV - 1], yyN, YYRules.getRule(yyN), yyLen[yyN]);
                    yyVal = yyV > yyTop ? null : yyVals[yyV]; // yyVal = yyDefault(yyV > yyTop ? null : yyVals[yyV]);
                    switch (yyN)
                    {
                        case 17:
                            case_17();
                            break;
                        case 18:
#line 529 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.SetSubSystem((int)yyVals[0 + yyTop]);
                            }
                            break;
                        case 19:
#line 533 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.SetCorFlags((int)yyVals[0 + yyTop]);
                            }
                            break;
                        case 21:
#line 538 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.SetImageBase((long)yyVals[0 + yyTop]);
                            }
                            break;
                        case 22:
#line 542 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.SetStackReserve((long)yyVals[0 + yyTop]);
                            }
                            break;
                        case 38:
#line 573 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.CurrentNameSpace = null;
                            }
                            break;
                        case 39:
#line 579 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.CurrentNameSpace = (string)yyVals[0 + yyTop];
                            }
                            break;
                        case 40:
#line 585 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.EndTypeDef();
                            }
                            break;
                        case 41:
                            case_41();
                            break;
                        case 42:
                            case_42();
                            break;
                        case 43:
#line 608 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-1 + yyTop] | TypeAttr.Public; }
                            break;
                        case 44:
#line 609 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-1 + yyTop] | TypeAttr.Private; }
                            break;
                        case 45:
#line 610 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-2 + yyTop] | TypeAttr.NestedPrivate; }
                            break;
                        case 46:
#line 611 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-2 + yyTop] | TypeAttr.NestedPublic; }
                            break;
                        case 47:
#line 612 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-2 + yyTop] | TypeAttr.NestedFamily; }
                            break;
                        case 48:
#line 613 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-2 + yyTop] | TypeAttr.NestedAssembly; }
                            break;
                        case 49:
#line 614 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-2 + yyTop] | TypeAttr.NestedFamAndAssem; }
                            break;
                        case 50:
#line 615 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-2 + yyTop] | TypeAttr.NestedFamOrAssem; }
                            break;
                        case 51:
#line 616 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { is_value_class = true; }
                            break;
                        case 52:
#line 617 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { is_enum_class = true; }
                            break;
                        case 53:
#line 618 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-1 + yyTop] | TypeAttr.Interface; }
                            break;
                        case 54:
#line 619 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-1 + yyTop] | TypeAttr.Sealed; }
                            break;
                        case 55:
#line 620 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-1 + yyTop] | TypeAttr.Abstract; }
                            break;
                        case 56:
#line 621 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { }
                            break;
                        case 57:
#line 622 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-1 + yyTop] | TypeAttr.SequentialLayout; }
                            break;
                        case 58:
#line 623 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-1 + yyTop] | TypeAttr.ExplicitLayout; }
                            break;
                        case 59:
#line 624 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { }
                            break;
                        case 60:
#line 625 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-1 + yyTop] | TypeAttr.UnicodeClass; }
                            break;
                        case 61:
#line 626 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-1 + yyTop] | TypeAttr.AutoClass; }
                            break;
                        case 62:
#line 627 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-1 + yyTop] | TypeAttr.Import; }
                            break;
                        case 63:
#line 628 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-1 + yyTop] | TypeAttr.Serializable; }
                            break;
                        case 64:
#line 629 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-1 + yyTop] | TypeAttr.BeforeFieldInit; }
                            break;
                        case 65:
#line 630 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-1 + yyTop] | TypeAttr.SpecialName; }
                            break;
                        case 66:
#line 631 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-1 + yyTop] | TypeAttr.RTSpecialName; }
                            break;
                        case 68:
#line 638 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = yyVals[0 + yyTop];
                            }
                            break;
                        case 71:
                            case_71();
                            break;
                        case 72:
                            case_72();
                            break;
                        case 74:
#line 664 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = yyVals[-1 + yyTop];
                            }
                            break;
                        case 76:
#line 671 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = yyVals[-1 + yyTop];
                            }
                            break;
                        case 77:
                            case_77();
                            break;
                        case 78:
                            case_78();
                            break;
                        case 80:
#line 691 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = yyVals[-1 + yyTop];
                            }
                            break;
                        case 81:
                            case_81();
                            break;
                        case 82:
                            case_82();
                            break;
                        case 83:
#line 712 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = yyVals[0 + yyTop];
                            }
                            break;
                        case 84:
#line 716 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new PrimitiveTypeRef(PrimitiveType.Object, "System.Object");
                            }
                            break;
                        case 85:
                            case_85();
                            break;
                        case 86:
                            case_86();
                            break;
                        case 87:
                            case_87();
                            break;
                        case 88:
                            case_88();
                            break;
                        case 89:
                            case_89();
                            break;
                        case 90:
                            case_90();
                            break;
                        case 91:
                            case_91();
                            break;
                        case 92:
#line 773 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new PEAPI.GenericParamAttributes();
                            }
                            break;
                        case 93:
#line 777 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (PEAPI.GenericParamAttributes)yyVals[-1 + yyTop] | PEAPI.GenericParamAttributes.Covariant;
                            }
                            break;
                        case 94:
#line 781 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (PEAPI.GenericParamAttributes)yyVals[-1 + yyTop] | PEAPI.GenericParamAttributes.Contravariant;
                            }
                            break;
                        case 95:
#line 785 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (PEAPI.GenericParamAttributes)yyVals[-1 + yyTop] | PEAPI.GenericParamAttributes.DefaultConstructorConstrait;
                            }
                            break;
                        case 96:
#line 789 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (PEAPI.GenericParamAttributes)yyVals[-1 + yyTop] | PEAPI.GenericParamAttributes.NotNullableValueTypeConstraint;
                            }
                            break;
                        case 97:
#line 793 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (PEAPI.GenericParamAttributes)yyVals[-1 + yyTop] | PEAPI.GenericParamAttributes.ReferenceTypeConstraint;
                            }
                            break;
                        case 98:
#line 799 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = yyVals[0 + yyTop];
                            }
                            break;
                        case 99:
                            case_99();
                            break;
                        case 100:
                            case_100();
                            break;
                        case 101:
                            case_101();
                            break;
                        case 102:
                            case_102();
                            break;
                        case 104:
#line 840 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = String.Format("{0}/{1}", yyVals[-2 + yyTop], yyVals[0 + yyTop]);
                            }
                            break;
                        case 105:
                            case_105();
                            break;
                        case 106:
                            case_106();
                            break;
                        case 107:
                            case_107();
                            break;
                        case 116:
#line 884 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                AddSecDecl(yyVals[0 + yyTop], false);
                            }
                            break;
                        case 118:
                            case_118();
                            break;
                        case 120:
#line 895 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.CurrentTypeDef.SetSize((int)yyVals[0 + yyTop]);
                            }
                            break;
                        case 121:
#line 899 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.CurrentTypeDef.SetPack((int)yyVals[0 + yyTop]);
                            }
                            break;
                        case 122:
                            case_122();
                            break;
                        case 125:
#line 932 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = GetTypeRef((BaseTypeRef)yyVals[0 + yyTop]);
                            }
                            break;
                        case 126:
                            case_126();
                            break;
                        case 127:
                            case_127();
                            break;
                        case 128:
                            case_128();
                            break;
                        case 129:
                            case_129();
                            break;
                        case 130:
                            case_130();
                            break;
                        case 131:
                            case_131();
                            break;
                        case 132:
                            case_132();
                            break;
                        case 133:
                            case_133();
                            break;
                        case 134:
                            case_134();
                            break;
                        case 135:
                            case_135();
                            break;
                        case 136:
#line 1006 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new MethodPointerTypeRef((CallConv)yyVals[-5 + yyTop], (BaseTypeRef)yyVals[-4 + yyTop], (ArrayList)yyVals[-1 + yyTop]);
                            }
                            break;
                        case 138:
#line 1013 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new PrimitiveTypeRef(PrimitiveType.Int8, "System.SByte");
                            }
                            break;
                        case 139:
#line 1017 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new PrimitiveTypeRef(PrimitiveType.Int16, "System.Int16");
                            }
                            break;
                        case 140:
#line 1021 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new PrimitiveTypeRef(PrimitiveType.Int32, "System.Int32");
                            }
                            break;
                        case 141:
#line 1025 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new PrimitiveTypeRef(PrimitiveType.Int64, "System.Int64");
                            }
                            break;
                        case 142:
#line 1029 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new PrimitiveTypeRef(PrimitiveType.Float32, "System.Single");
                            }
                            break;
                        case 143:
#line 1033 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new PrimitiveTypeRef(PrimitiveType.Float64, "System.Double");
                            }
                            break;
                        case 144:
#line 1037 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new PrimitiveTypeRef(PrimitiveType.UInt8, "System.Byte");
                            }
                            break;
                        case 145:
#line 1041 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new PrimitiveTypeRef(PrimitiveType.UInt8, "System.Byte");
                            }
                            break;
                        case 146:
#line 1045 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new PrimitiveTypeRef(PrimitiveType.UInt16, "System.UInt16");
                            }
                            break;
                        case 147:
#line 1049 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new PrimitiveTypeRef(PrimitiveType.UInt16, "System.UInt16");
                            }
                            break;
                        case 148:
#line 1053 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new PrimitiveTypeRef(PrimitiveType.UInt32, "System.UInt32");
                            }
                            break;
                        case 149:
#line 1057 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new PrimitiveTypeRef(PrimitiveType.UInt32, "System.UInt32");
                            }
                            break;
                        case 150:
#line 1061 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new PrimitiveTypeRef(PrimitiveType.UInt64, "System.UInt64");
                            }
                            break;
                        case 151:
#line 1065 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new PrimitiveTypeRef(PrimitiveType.UInt64, "System.UInt64");
                            }
                            break;
                        case 152:
                            case_152();
                            break;
                        case 153:
#line 1074 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new PrimitiveTypeRef(PrimitiveType.NativeUInt, "System.UIntPtr");
                            }
                            break;
                        case 154:
#line 1078 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new PrimitiveTypeRef(PrimitiveType.NativeUInt, "System.UIntPtr");
                            }
                            break;
                        case 155:
                            case_155();
                            break;
                        case 156:
#line 1087 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new PrimitiveTypeRef(PrimitiveType.Char, "System.Char");
                            }
                            break;
                        case 157:
#line 1091 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new PrimitiveTypeRef(PrimitiveType.Char, "System.Char");
                            }
                            break;
                        case 158:
#line 1095 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new PrimitiveTypeRef(PrimitiveType.Void, "System.Void");
                            }
                            break;
                        case 159:
#line 1099 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new PrimitiveTypeRef(PrimitiveType.Boolean, "System.Boolean");
                            }
                            break;
                        case 160:
#line 1103 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new PrimitiveTypeRef(PrimitiveType.String, "System.String");
                            }
                            break;
                        case 161:
                            case_161();
                            break;
                        case 162:
                            case_162();
                            break;
                        case 163:
                            case_163();
                            break;
                        case 164:
                            case_164();
                            break;
                        case 165:
                            case_165();
                            break;
                        case 166:
                            case_166();
                            break;
                        case 167:
                            case_167();
                            break;
                        case 168:
#line 1160 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (CallConv)yyVals[0 + yyTop] | CallConv.Instance;
                            }
                            break;
                        case 169:
#line 1164 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (CallConv)yyVals[0 + yyTop] | CallConv.InstanceExplicit;
                            }
                            break;
                        case 171:
#line 1171 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new CallConv();
                            }
                            break;
                        case 172:
#line 1175 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = CallConv.Default;
                            }
                            break;
                        case 173:
#line 1179 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = CallConv.Vararg;
                            }
                            break;
                        case 174:
#line 1183 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = CallConv.Cdecl;
                            }
                            break;
                        case 175:
#line 1187 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = CallConv.Stdcall;
                            }
                            break;
                        case 176:
#line 1191 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = CallConv.Thiscall;
                            }
                            break;
                        case 177:
#line 1195 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = CallConv.Fastcall;
                            }
                            break;
                        case 179:
#line 1202 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new CustomMarshaller((string)yyVals[-3 + yyTop], (string)yyVals[-1 + yyTop]);
                            }
                            break;
                        case 180:
#line 1206 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new FixedSysString((uint)(int)yyVals[-1 + yyTop]);
                            }
                            break;
                        case 181:
#line 1210 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new FixedArray((int)yyVals[-1 + yyTop]);
                            }
                            break;
                        case 183:
#line 1215 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.Currency;
                            }
                            break;
                        case 185:
#line 1220 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.Void;
                            }
                            break;
                        case 186:
#line 1224 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.Boolean;
                            }
                            break;
                        case 187:
#line 1228 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.Int8;
                            }
                            break;
                        case 188:
#line 1232 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.Int16;
                            }
                            break;
                        case 189:
#line 1236 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.Int32;
                            }
                            break;
                        case 190:
#line 1240 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.Int64;
                            }
                            break;
                        case 191:
#line 1244 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.Float32;
                            }
                            break;
                        case 192:
#line 1248 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.Float64;
                            }
                            break;
                        case 193:
#line 1252 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.Error;
                            }
                            break;
                        case 194:
#line 1256 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.UInt8;
                            }
                            break;
                        case 195:
#line 1260 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.UInt8;
                            }
                            break;
                        case 196:
#line 1264 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.UInt16;
                            }
                            break;
                        case 197:
#line 1268 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.UInt16;
                            }
                            break;
                        case 198:
#line 1272 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.UInt32;
                            }
                            break;
                        case 199:
#line 1276 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.UInt32;
                            }
                            break;
                        case 200:
#line 1280 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.UInt64;
                            }
                            break;
                        case 201:
#line 1284 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.UInt64;
                            }
                            break;
                        case 203:
#line 1289 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new NativeArray((NativeType)yyVals[-2 + yyTop]);
                            }
                            break;
                        case 204:
#line 1293 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new NativeArray((NativeType)yyVals[-3 + yyTop], (int)yyVals[-1 + yyTop], 0, 0);
                            }
                            break;
                        case 205:
                            case_205();
                            break;
                        case 206:
                            case_206();
                            break;
                        case 209:
#line 1309 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.BStr;
                            }
                            break;
                        case 210:
#line 1313 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.LPStr;
                            }
                            break;
                        case 211:
#line 1317 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.LPWStr;
                            }
                            break;
                        case 212:
#line 1321 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.LPTStr;
                            }
                            break;
                        case 213:
#line 1325 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.ByValStr;
                            }
                            break;
                        case 215:
#line 1330 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.IUnknown;
                            }
                            break;
                        case 216:
#line 1334 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.IDispatch;
                            }
                            break;
                        case 217:
#line 1338 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.Struct;
                            }
                            break;
                        case 218:
#line 1342 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.Interface;
                            }
                            break;
                        case 219:
                            case_219();
                            break;
                        case 221:
#line 1354 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.Int;
                            }
                            break;
                        case 222:
#line 1358 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.UInt;
                            }
                            break;
                        case 224:
#line 1363 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.ByValStr;
                            }
                            break;
                        case 225:
#line 1367 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.AnsiBStr;
                            }
                            break;
                        case 226:
#line 1371 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.TBstr;
                            }
                            break;
                        case 227:
#line 1375 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.VariantBool;
                            }
                            break;
                        case 228:
#line 1379 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.FuncPtr;
                            }
                            break;
                        case 229:
#line 1383 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.AsAny;
                            }
                            break;
                        case 230:
#line 1387 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = NativeType.LPStruct;
                            }
                            break;
                        case 233:
#line 1395 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = SafeArrayType.variant;
                            }
                            break;
                        case 234:
#line 1399 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = SafeArrayType.currency;
                            }
                            break;
                        case 236:
#line 1404 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = SafeArrayType.boolean;
                            }
                            break;
                        case 237:
#line 1408 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = SafeArrayType.int8;
                            }
                            break;
                        case 238:
#line 1412 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = SafeArrayType.int16;
                            }
                            break;
                        case 239:
#line 1416 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = SafeArrayType.int32;
                            }
                            break;
                        case 241:
#line 1421 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = SafeArrayType.float32;
                            }
                            break;
                        case 242:
#line 1425 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = SafeArrayType.float64;
                            }
                            break;
                        case 243:
#line 1429 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = SafeArrayType.uint8;
                            }
                            break;
                        case 244:
#line 1433 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = SafeArrayType.uint16;
                            }
                            break;
                        case 245:
#line 1437 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = SafeArrayType.uint32;
                            }
                            break;
                        case 251:
#line 1446 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = SafeArrayType.Decimal;
                            }
                            break;
                        case 252:
#line 1450 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = SafeArrayType.date;
                            }
                            break;
                        case 253:
#line 1454 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = SafeArrayType.bstr;
                            }
                            break;
                        case 256:
#line 1460 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = SafeArrayType.unknown;
                            }
                            break;
                        case 257:
#line 1464 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = SafeArrayType.unknown;
                            }
                            break;
                        case 259:
#line 1469 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = SafeArrayType.Int;
                            }
                            break;
                        case 260:
#line 1473 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = SafeArrayType.UInt;
                            }
                            break;
                        case 261:
#line 1477 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = SafeArrayType.error;
                            }
                            break;
                        case 277:
                            case_277();
                            break;
                        case 279:
#line 1523 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = yyVals[-1 + yyTop];
                            }
                            break;
                        case 280:
#line 1529 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new FieldAttr();
                            }
                            break;
                        case 281:
#line 1533 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (FieldAttr)yyVals[-1 + yyTop] | FieldAttr.Public;
                            }
                            break;
                        case 282:
#line 1537 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (FieldAttr)yyVals[-1 + yyTop] | FieldAttr.Private;
                            }
                            break;
                        case 283:
#line 1541 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (FieldAttr)yyVals[-1 + yyTop] | FieldAttr.Family;
                            }
                            break;
                        case 284:
#line 1545 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (FieldAttr)yyVals[-1 + yyTop] | FieldAttr.Assembly;
                            }
                            break;
                        case 285:
#line 1549 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (FieldAttr)yyVals[-1 + yyTop] | FieldAttr.FamAndAssem;
                            }
                            break;
                        case 286:
#line 1553 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (FieldAttr)yyVals[-1 + yyTop] | FieldAttr.FamOrAssem;
                            }
                            break;
                        case 287:
#line 1557 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                /* This is just 0x0000*/
                            }
                            break;
                        case 288:
#line 1561 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (FieldAttr)yyVals[-1 + yyTop] | FieldAttr.Static;
                            }
                            break;
                        case 289:
#line 1565 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (FieldAttr)yyVals[-1 + yyTop] | FieldAttr.Initonly;
                            }
                            break;
                        case 290:
#line 1569 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (FieldAttr)yyVals[-1 + yyTop] | FieldAttr.RTSpecialName;
                            }
                            break;
                        case 291:
#line 1573 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (FieldAttr)yyVals[-1 + yyTop] | FieldAttr.SpecialName;
                            }
                            break;
                        case 292:
                            case_292();
                            break;
                        case 293:
#line 1582 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (FieldAttr)yyVals[-1 + yyTop] | FieldAttr.Literal;
                            }
                            break;
                        case 294:
#line 1586 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (FieldAttr)yyVals[-1 + yyTop] | FieldAttr.Notserialized;
                            }
                            break;
                        case 296:
#line 1593 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = yyVals[0 + yyTop];
                            }
                            break;
                        case 298:
#line 1600 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = yyVals[0 + yyTop];
                            }
                            break;
                        case 299:
#line 1606 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new FloatConst(Convert.ToSingle(yyVals[-1 + yyTop]));
                            }
                            break;
                        case 300:
#line 1610 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new DoubleConst(Convert.ToDouble(yyVals[-1 + yyTop]));
                            }
                            break;
                        case 301:
#line 1614 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new FloatConst(BitConverter.ToSingle(BitConverter.GetBytes((long)yyVals[-1 + yyTop]), BitConverter.IsLittleEndian ? 0 : 4));
                            }
                            break;
                        case 302:
#line 1618 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new DoubleConst(BitConverter.Int64BitsToDouble((long)yyVals[-1 + yyTop]));
                            }
                            break;
                        case 303:
#line 1622 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new IntConst(Convert.ToInt64(yyVals[-1 + yyTop]));
                            }
                            break;
                        case 304:
#line 1626 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new UIntConst(Convert.ToUInt64((ulong)(long)yyVals[-1 + yyTop]));
                            }
                            break;
                        case 305:
#line 1630 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new IntConst((int)((long)yyVals[-1 + yyTop]));
                            }
                            break;
                        case 306:
#line 1634 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new UIntConst((uint)((long)yyVals[-1 + yyTop]));
                            }
                            break;
                        case 307:
#line 1638 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new IntConst((short)((long)yyVals[-1 + yyTop]));
                            }
                            break;
                        case 308:
#line 1642 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new UIntConst((ushort)((long)yyVals[-1 + yyTop]));
                            }
                            break;
                        case 309:
#line 1646 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new CharConst(Convert.ToChar(yyVals[-1 + yyTop]));
                            }
                            break;
                        case 310:
#line 1650 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new CharConst(Convert.ToChar(yyVals[-1 + yyTop]));
                            }
                            break;
                        case 311:
#line 1654 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new IntConst((sbyte)((long)(yyVals[-1 + yyTop])));
                            }
                            break;
                        case 312:
#line 1658 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new UIntConst((byte)((long)(yyVals[-1 + yyTop])));
                            }
                            break;
                        case 313:
#line 1662 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new BoolConst((bool)yyVals[-1 + yyTop]);
                            }
                            break;
                        case 315:
#line 1669 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new ByteArrConst((byte[])yyVals[0 + yyTop]);
                            }
                            break;
                        case 317:
                            case_317();
                            break;
                        case 318:
#line 1681 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new NullConst();
                            }
                            break;
                        case 320:
#line 1688 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new StringConst((string)yyVals[-1 + yyTop]);
                            }
                            break;
                        case 322:
#line 1699 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new List<BoolConst>() { new BoolConst((bool)yyVals[0 + yyTop]) };
                            }
                            break;
                        case 323:
                            case_323();
                            break;
                        case 324:
                            case_324();
                            break;
                        case 325:
#line 1730 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new DataDef((string)yyVals[-1 + yyTop], (DataSegment)yyVals[-2 + yyTop]);
                            }
                            break;
                        case 326:
#line 1734 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new DataDef(String.Empty, (DataSegment)yyVals[0 + yyTop]);
                            }
                            break;
                        case 327:
#line 1737 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = DataSegment.Data; }
                            break;
                        case 328:
#line 1738 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = DataSegment.TLS; }
                            break;
                        case 329:
#line 1739 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = DataSegment.CIL; }
                            break;
                        case 330:
#line 1745 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = yyVals[-1 + yyTop];
                            }
                            break;
                        case 332:
                            case_332();
                            break;
                        case 333:
                            case_333();
                            break;
                        case 334:
#line 1765 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new StringConst((string)yyVals[-1 + yyTop]);
                            }
                            break;
                        case 335:
#line 1769 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new StringConst((string)yyVals[-1 + yyTop]);
                            }
                            break;
                        case 336:
                            case_336();
                            break;
                        case 337:
#line 1778 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new ByteArrConst((byte[])yyVals[0 + yyTop]);
                            }
                            break;
                        case 338:
                            case_338();
                            break;
                        case 339:
                            case_339();
                            break;
                        case 340:
                            case_340();
                            break;
                        case 341:
                            case_341();
                            break;
                        case 342:
                            case_342();
                            break;
                        case 343:
                            case_343();
                            break;
                        case 344:
                            case_344();
                            break;
                        case 345:
                            case_345();
                            break;
                        case 346:
                            case_346();
                            break;
                        case 347:
                            case_347();
                            break;
                        case 348:
                            case_348();
                            break;
                        case 349:
                            case_349();
                            break;
                        case 350:
                            case_350();
                            break;
                        case 351:
#line 1900 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.EndMethodDef(tokenizer.Location);
                            }
                            break;
                        case 352:
                            case_352();
                            break;
                        case 353:
                            case_353();
                            break;
                        case 354:
#line 1939 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = new MethAttr(); }
                            break;
                        case 355:
#line 1940 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (MethAttr)yyVals[-1 + yyTop] | MethAttr.Static; }
                            break;
                        case 356:
#line 1941 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (MethAttr)yyVals[-1 + yyTop] | MethAttr.Public; }
                            break;
                        case 357:
#line 1942 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (MethAttr)yyVals[-1 + yyTop] | MethAttr.Private; }
                            break;
                        case 358:
#line 1943 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (MethAttr)yyVals[-1 + yyTop] | MethAttr.Family; }
                            break;
                        case 359:
#line 1944 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (MethAttr)yyVals[-1 + yyTop] | MethAttr.Assembly; }
                            break;
                        case 360:
#line 1945 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (MethAttr)yyVals[-1 + yyTop] | MethAttr.FamAndAssem; }
                            break;
                        case 361:
#line 1946 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (MethAttr)yyVals[-1 + yyTop] | MethAttr.FamOrAssem; }
                            break;
                        case 362:
#line 1947 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { /* CHECK HEADERS */ }
                            break;
                        case 363:
#line 1948 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (MethAttr)yyVals[-1 + yyTop] | MethAttr.Final; }
                            break;
                        case 364:
#line 1949 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (MethAttr)yyVals[-1 + yyTop] | MethAttr.Virtual; }
                            break;
                        case 365:
#line 1950 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (MethAttr)yyVals[-1 + yyTop] | MethAttr.Abstract; }
                            break;
                        case 366:
#line 1951 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (MethAttr)yyVals[-1 + yyTop] | MethAttr.HideBySig; }
                            break;
                        case 367:
#line 1952 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (MethAttr)yyVals[-1 + yyTop] | MethAttr.NewSlot; }
                            break;
                        case 368:
#line 1953 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (MethAttr)yyVals[-1 + yyTop] | MethAttr.RequireSecObject; }
                            break;
                        case 369:
#line 1954 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (MethAttr)yyVals[-1 + yyTop] | MethAttr.SpecialName; }
                            break;
                        case 370:
#line 1955 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (MethAttr)yyVals[-1 + yyTop] | MethAttr.RTSpecialName; }
                            break;
                        case 371:
#line 1956 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (MethAttr)yyVals[-1 + yyTop] | MethAttr.Strict; }
                            break;
                        case 372:
#line 1957 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { /* Do nothing */ }
                            break;
                        case 374:
                            case_374();
                            break;
                        case 375:
                            case_375();
                            break;
                        case 376:
                            case_376();
                            break;
                        case 377:
#line 1983 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = new PInvokeAttr(); }
                            break;
                        case 378:
#line 1984 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (PInvokeAttr)yyVals[-1 + yyTop] | PInvokeAttr.nomangle; }
                            break;
                        case 379:
#line 1985 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (PInvokeAttr)yyVals[-1 + yyTop] | PInvokeAttr.ansi; }
                            break;
                        case 380:
#line 1986 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (PInvokeAttr)yyVals[-1 + yyTop] | PInvokeAttr.unicode; }
                            break;
                        case 381:
#line 1987 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (PInvokeAttr)yyVals[-1 + yyTop] | PInvokeAttr.autochar; }
                            break;
                        case 382:
#line 1988 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (PInvokeAttr)yyVals[-1 + yyTop] | PInvokeAttr.lasterr; }
                            break;
                        case 383:
#line 1989 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (PInvokeAttr)yyVals[-1 + yyTop] | PInvokeAttr.winapi; }
                            break;
                        case 384:
#line 1990 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (PInvokeAttr)yyVals[-1 + yyTop] | PInvokeAttr.cdecl; }
                            break;
                        case 385:
#line 1991 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (PInvokeAttr)yyVals[-1 + yyTop] | PInvokeAttr.stdcall; }
                            break;
                        case 386:
#line 1992 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (PInvokeAttr)yyVals[-1 + yyTop] | PInvokeAttr.thiscall; }
                            break;
                        case 387:
#line 1993 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (PInvokeAttr)yyVals[-1 + yyTop] | PInvokeAttr.fastcall; }
                            break;
                        case 388:
#line 1994 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (PInvokeAttr)yyVals[-3 + yyTop] | PInvokeAttr.bestfit_on; }
                            break;
                        case 389:
#line 1995 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (PInvokeAttr)yyVals[-3 + yyTop] | PInvokeAttr.bestfit_off; }
                            break;
                        case 390:
#line 1996 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (PInvokeAttr)yyVals[-3 + yyTop] | PInvokeAttr.charmaperror_on; }
                            break;
                        case 391:
#line 1997 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (PInvokeAttr)yyVals[-3 + yyTop] | PInvokeAttr.charmaperror_off; }
                            break;
                        case 395:
#line 2005 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = new ParamAttr(); }
                            break;
                        case 396:
#line 2006 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (ParamAttr)yyVals[-3 + yyTop] | ParamAttr.In; }
                            break;
                        case 397:
#line 2007 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (ParamAttr)yyVals[-3 + yyTop] | ParamAttr.Out; }
                            break;
                        case 398:
#line 2008 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (ParamAttr)yyVals[-3 + yyTop] | ParamAttr.Opt; }
                            break;
                        case 399:
#line 2011 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = new ImplAttr(); }
                            break;
                        case 400:
#line 2012 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (ImplAttr)yyVals[-1 + yyTop] | ImplAttr.Native; }
                            break;
                        case 401:
#line 2013 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (ImplAttr)yyVals[-1 + yyTop] | ImplAttr.IL; }
                            break;
                        case 402:
#line 2014 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (ImplAttr)yyVals[-1 + yyTop] | ImplAttr.IL; }
                            break;
                        case 403:
#line 2015 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (ImplAttr)yyVals[-1 + yyTop] | ImplAttr.Optil; }
                            break;
                        case 404:
#line 2016 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { /* should this reset? */ }
                            break;
                        case 405:
#line 2017 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (ImplAttr)yyVals[-1 + yyTop] | ImplAttr.Unmanaged; }
                            break;
                        case 406:
#line 2018 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (ImplAttr)yyVals[-1 + yyTop] | ImplAttr.ForwardRef; }
                            break;
                        case 407:
#line 2019 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (ImplAttr)yyVals[-1 + yyTop] | ImplAttr.PreserveSig; }
                            break;
                        case 408:
#line 2020 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (ImplAttr)yyVals[-1 + yyTop] | ImplAttr.Runtime; }
                            break;
                        case 409:
#line 2021 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (ImplAttr)yyVals[-1 + yyTop] | ImplAttr.InternalCall; }
                            break;
                        case 410:
#line 2022 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (ImplAttr)yyVals[-1 + yyTop] | ImplAttr.Synchronised; }
                            break;
                        case 411:
#line 2023 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (ImplAttr)yyVals[-1 + yyTop] | ImplAttr.NoInLining; }
                            break;
                        case 412:
#line 2024 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (ImplAttr)yyVals[-1 + yyTop] | ImplAttr.NoOptimization; }
                            break;
                        case 413:
#line 2025 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (ImplAttr)yyVals[-1 + yyTop] | ImplAttr.AggressiveInlining; }
                            break;
                        case 416:
                            case_416();
                            break;
                        case 417:
                            case_417();
                            break;
                        case 418:
#line 2049 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new ParamDef((ParamAttr)yyVals[-1 + yyTop], null, (BaseTypeRef)yyVals[0 + yyTop]);
                            }
                            break;
                        case 419:
#line 2053 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new ParamDef((ParamAttr)yyVals[-2 + yyTop], (string)yyVals[0 + yyTop], (BaseTypeRef)yyVals[-1 + yyTop]);
                            }
                            break;
                        case 420:
                            case_420();
                            break;
                        case 421:
                            case_421();
                            break;
                        case 422:
                            case_422();
                            break;
                        case 423:
#line 2078 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new ArrayList(0);
                            }
                            break;
                        case 424:
                            case_424();
                            break;
                        case 425:
                            case_425();
                            break;
                        case 426:
                            case_426();
                            break;
                        case 427:
                            case_427();
                            break;
                        case 432:
                            case_432();
                            break;
                        case 433:
#line 2123 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.CurrentMethodDef.SetMaxStack((int)yyVals[0 + yyTop]);
                            }
                            break;
                        case 434:
                            case_434();
                            break;
                        case 435:
                            case_435();
                            break;
                        case 436:
                            case_436();
                            break;
                        case 437:
#line 2147 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.CurrentMethodDef.ZeroInit();
                            }
                            break;
                        case 441:
                            case_441();
                            break;
                        case 442:
                            case_442();
                            break;
                        case 443:
                            case_443();
                            break;
                        case 445:
                            case_445();
                            break;
                        case 447:
#line 2203 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.CurrentMethodDef.AddLabel((string)yyVals[-1 + yyTop]);
                            }
                            break;
                        case 450:
#line 2209 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                AddSecDecl(yyVals[0 + yyTop], false);
                            }
                            break;
                        case 453:
                            case_453();
                            break;
                        case 456:
                            case_456();
                            break;
                        case 457:
                            case_457();
                            break;
                        case 458:
#line 2237 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new Local(-1, (BaseTypeRef)yyVals[0 + yyTop]);
                            }
                            break;
                        case 459:
#line 2241 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new Local(-1, (string)yyVals[0 + yyTop], (BaseTypeRef)yyVals[-1 + yyTop]);
                            }
                            break;
                        case 460:
#line 2245 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new Local((int)yyVals[-1 + yyTop], (BaseTypeRef)yyVals[0 + yyTop]);
                            }
                            break;
                        case 461:
#line 2249 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new Local((int)yyVals[-2 + yyTop], (string)yyVals[0 + yyTop], (BaseTypeRef)yyVals[-1 + yyTop]);
                            }
                            break;
                        case 462:
#line 2255 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = yyVals[-1 + yyTop];
                            }
                            break;
                        case 463:
                            case_463();
                            break;
                        case 464:
                            case_464();
                            break;
                        case 466:
                            case_466();
                            break;
                        case 467:
                            case_467();
                            break;
                        case 468:
                            case_468();
                            break;
                        case 469:
#line 2308 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new TryBlock((HandlerBlock)yyVals[0 + yyTop], tokenizer.Location);
                            }
                            break;
                        case 470:
                            case_470();
                            break;
                        case 471:
                            case_471();
                            break;
                        case 472:
                            case_472();
                            break;
                        case 473:
                            case_473();
                            break;
                        case 474:
                            case_474();
                            break;
                        case 475:
                            case_475();
                            break;
                        case 476:
                            case_476();
                            break;
                        case 477:
                            case_477();
                            break;
                        case 478:
                            case_478();
                            break;
                        case 479:
                            case_479();
                            break;
                        case 480:
                            case_480();
                            break;
                        case 481:
#line 2390 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {

                            }
                            break;
                        case 482:
                            case_482();
                            break;
                        case 483:
                            case_483();
                            break;
                        case 484:
                            case_484();
                            break;
                        case 485:
                            case_485();
                            break;
                        case 486:
                            case_486();
                            break;
                        case 487:
                            case_487();
                            break;
                        case 488:
                            case_488();
                            break;
                        case 489:
                            case_489();
                            break;
                        case 490:
                            case_490();
                            break;
                        case 491:
                            case_491();
                            break;
                        case 492:
                            case_492();
                            break;
                        case 493:
                            case_493();
                            break;
                        case 494:
                            case_494();
                            break;
                        case 495:
                            case_495();
                            break;
                        case 496:
                            case_496();
                            break;
                        case 497:
                            case_497();
                            break;
                        case 498:
                            case_498();
                            break;
                        case 499:
                            case_499();
                            break;
                        case 500:
                            case_500();
                            break;
                        case 501:
                            case_501();
                            break;
                        case 502:
                            case_502();
                            break;
                        case 503:
                            case_503();
                            break;
                        case 504:
                            case_504();
                            break;
                        case 505:
                            case_505();
                            break;
                        case 506:
#line 2588 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.CurrentMethodDef.AddInstr(new SwitchInstr((ArrayList)yyVals[-1 + yyTop], tokenizer.Location));
                            }
                            break;
                        case 507:
                            case_507();
                            break;
                        case 508:
                            case_508();
                            break;
                        case 510:
                            case_510();
                            break;
                        case 511:
                            case_511();
                            break;
                        case 512:
                            case_512();
                            break;
                        case 513:
                            case_513();
                            break;
                        case 516:
#line 2678 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = yyVals[0 + yyTop];
                            }
                            break;
                        case 517:
                            case_517();
                            break;
                        case 518:
#line 2689 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = codegen.GetGlobalFieldRef((BaseTypeRef)yyVals[-1 + yyTop], (string)yyVals[0 + yyTop]);
                            }
                            break;
                        case 519:
#line 2695 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.CurrentTypeDef.EndEventDef();
                            }
                            break;
                        case 520:
                            case_520();
                            break;
                        case 522:
#line 2711 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new FeatureAttr();
                            }
                            break;
                        case 523:
#line 2715 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (FeatureAttr)yyVals[-1 + yyTop] & FeatureAttr.Rtspecialname;
                            }
                            break;
                        case 524:
#line 2719 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (FeatureAttr)yyVals[-1 + yyTop] & FeatureAttr.Specialname;
                            }
                            break;
                        case 527:
                            case_527();
                            break;
                        case 528:
                            case_528();
                            break;
                        case 529:
                            case_529();
                            break;
                        case 530:
                            case_530();
                            break;
                        case 531:
                            case_531();
                            break;
                        case 534:
#line 2758 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.CurrentTypeDef.EndPropertyDef();
                            }
                            break;
                        case 535:
                            case_535();
                            break;
                        case 536:
#line 2777 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new FeatureAttr();
                            }
                            break;
                        case 537:
#line 2781 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (FeatureAttr)yyVals[-1 + yyTop] | FeatureAttr.Rtspecialname;
                            }
                            break;
                        case 538:
#line 2785 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (FeatureAttr)yyVals[-1 + yyTop] | FeatureAttr.Specialname;
                            }
                            break;
                        case 539:
#line 2789 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (FeatureAttr)yyVals[-1 + yyTop] | FeatureAttr.Instance;
                            }
                            break;
                        case 542:
#line 2799 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.CurrentTypeDef.CurrentProperty.AddSet((MethodRef)yyVals[0 + yyTop]);
                            }
                            break;
                        case 543:
#line 2803 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.CurrentTypeDef.CurrentProperty.AddGet((MethodRef)yyVals[0 + yyTop]);
                            }
                            break;
                        case 544:
#line 2807 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.CurrentTypeDef.CurrentProperty.AddOther((MethodRef)yyVals[0 + yyTop]);
                            }
                            break;
                        case 545:
                            case_545();
                            break;
                        case 548:
#line 2821 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new CustomAttr((BaseMethodRef)yyVals[0 + yyTop], null);
                            }
                            break;
                        case 550:
#line 2826 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new CustomAttr((BaseMethodRef)yyVals[-2 + yyTop], new ByteArrConst((byte[])yyVals[0 + yyTop]));
                            }
                            break;
                        case 551:
#line 2830 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new CustomAttr((BaseMethodRef)yyVals[-4 + yyTop], (PEAPI.Constant)yyVals[-1 + yyTop]);
                            }
                            break;
                        case 555:
                            case_555();
                            break;
                        case 556:
#line 2851 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new ByteArrConst((byte[])yyVals[0 + yyTop]);
                            }
                            break;
                        case 557:
#line 2855 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new StringConst((string)yyVals[-1 + yyTop]);
                            }
                            break;
                        case 558:
                            case_558();
                            break;
                        case 560:
                            case_560();
                            break;
                        case 562:
#line 2884 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new StringConst(((TypeRef)yyVals[-1 + yyTop]).FullName);
                            }
                            break;
                        case 563:
                            case_563();
                            break;
                        case 564:
                            case_564();
                            break;
                        case 565:
#line 2920 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = TypeSpecToPermPair(yyVals[-4 + yyTop], yyVals[-3 + yyTop], (ArrayList)yyVals[-1 + yyTop]);
                            }
                            break;
                        case 566:
#line 2924 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = TypeSpecToPermPair(yyVals[-1 + yyTop], yyVals[0 + yyTop], null);
                            }
                            break;
                        case 567:
                            case_567();
                            break;
                        case 568:
                            case_568();
                            break;
                        case 569:
#line 2941 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new MIPermissionSet((PEAPI.SecurityAction)yyVals[-4 + yyTop], (ArrayList)yyVals[-1 + yyTop]);
                            }
                            break;
                        case 570:
                            case_570();
                            break;
                        case 571:
                            case_571();
                            break;
                        case 572:
#line 2961 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new MIPermission((BaseTypeRef)yyVals[-4 + yyTop], (ArrayList)yyVals[-1 + yyTop]);
                            }
                            break;
                        case 573:
                            case_573();
                            break;
                        case 574:
                            case_574();
                            break;
                        case 575:
                            case_575();
                            break;
                        case 576:
                            case_576();
                            break;
                        case 577:
#line 2993 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new NameValuePair((string)yyVals[-2 + yyTop], (PEAPI.Constant)yyVals[0 + yyTop]);
                            }
                            break;
                        case 578:
#line 2999 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = MemberTypes.Property;
                            }
                            break;
                        case 579:
#line 3003 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = MemberTypes.Field;
                            }
                            break;
                        case 580:
                            case_580();
                            break;
                        case 581:
                            case_581();
                            break;
                        case 582:
#line 3023 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new NameValuePair((string)yyVals[-2 + yyTop], yyVals[0 + yyTop]);
                            }
                            break;
                        case 585:
#line 3031 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = yyVals[-1 + yyTop];
                            }
                            break;
                        case 587:
#line 3036 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = ClassRefToObject(yyVals[-5 + yyTop], (byte)(int)yyVals[-1 + yyTop]);
                            }
                            break;
                        case 588:
#line 3040 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = ClassRefToObject(yyVals[-5 + yyTop], (short)(int)yyVals[-1 + yyTop]);
                            }
                            break;
                        case 589:
#line 3044 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = ClassRefToObject(yyVals[-5 + yyTop], (int)yyVals[-1 + yyTop]);
                            }
                            break;
                        case 590:
#line 3048 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = ClassRefToObject(yyVals[-3 + yyTop], (int)yyVals[-1 + yyTop]);
                            }
                            break;
                        case 591:
#line 3054 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = PEAPI.SecurityAction.Request;
                            }
                            break;
                        case 592:
#line 3058 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = PEAPI.SecurityAction.Demand;
                            }
                            break;
                        case 593:
#line 3062 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = PEAPI.SecurityAction.Assert;
                            }
                            break;
                        case 594:
#line 3066 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = PEAPI.SecurityAction.Deny;
                            }
                            break;
                        case 595:
#line 3070 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = PEAPI.SecurityAction.PermitOnly;
                            }
                            break;
                        case 596:
#line 3074 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = PEAPI.SecurityAction.LinkDemand;
                            }
                            break;
                        case 597:
#line 3078 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = PEAPI.SecurityAction.InheritDemand;
                            }
                            break;
                        case 598:
#line 3082 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = PEAPI.SecurityAction.RequestMinimum;
                            }
                            break;
                        case 599:
#line 3086 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = PEAPI.SecurityAction.RequestOptional;
                            }
                            break;
                        case 600:
#line 3090 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = PEAPI.SecurityAction.RequestRefuse;
                            }
                            break;
                        case 601:
#line 3094 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = PEAPI.SecurityAction.PreJitGrant;
                            }
                            break;
                        case 602:
#line 3098 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = PEAPI.SecurityAction.PreJitDeny;
                            }
                            break;
                        case 603:
#line 3102 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = PEAPI.SecurityAction.NonCasDemand;
                            }
                            break;
                        case 604:
#line 3106 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = PEAPI.SecurityAction.NonCasLinkDemand;
                            }
                            break;
                        case 605:
#line 3110 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = PEAPI.SecurityAction.NonCasInheritance;
                            }
                            break;
                        case 606:
#line 3116 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                            }
                            break;
                        case 607:
#line 3120 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.SetModuleName((string)yyVals[0 + yyTop]);
                            }
                            break;
                        case 608:
#line 3124 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.ExternTable.AddModule((string)yyVals[0 + yyTop]);
                            }
                            break;
                        case 609:
#line 3131 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.SetFileRef(new FileRef((string)yyVals[-5 + yyTop], (byte[])yyVals[-1 + yyTop], (bool)yyVals[-6 + yyTop], (bool)yyVals[0 + yyTop]));
                            }
                            break;
                        case 610:
                            case_610();
                            break;
                        case 611:
#line 3142 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = true;
                            }
                            break;
                        case 612:
#line 3146 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = false;
                            }
                            break;
                        case 613:
#line 3152 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = false;
                            }
                            break;
                        case 614:
#line 3156 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = true;
                            }
                            break;
                        case 615:
                            case_615();
                            break;
                        case 616:
                            case_616();
                            break;
                        case 617:
#line 3177 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = new PEAPI.AssemAttr();
                            }
                            break;
                        case 618:
#line 3184 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = ((PEAPI.AssemAttr)yyVals[-1 + yyTop]) | PEAPI.AssemAttr.Retargetable;
                            }
                            break;
                        case 621:
#line 3194 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.ThisAssembly.SetPublicKey((byte[])yyVals[0 + yyTop]);
                            }
                            break;
                        case 622:
#line 3198 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.ThisAssembly.SetVersion((int)yyVals[-6 + yyTop], (int)yyVals[-4 + yyTop], (int)yyVals[-2 + yyTop], (int)yyVals[0 + yyTop]);
                            }
                            break;
                        case 623:
#line 3202 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.ThisAssembly.SetLocale((string)yyVals[0 + yyTop]);
                            }
                            break;
                        case 625:
#line 3207 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.ThisAssembly.SetHashAlgorithm((int)yyVals[0 + yyTop]);
                            }
                            break;
                        case 626:
#line 3211 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.ThisAssembly.AddCustomAttribute((CustomAttr)yyVals[0 + yyTop]);
                            }
                            break;
                        case 627:
#line 3215 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                AddSecDecl(yyVals[0 + yyTop], true);
                            }
                            break;
                        case 634:
                            case_634();
                            break;
                        case 635:
                            case_635();
                            break;
                        case 638:
#line 3251 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.CurrentAssemblyRef.SetVersion((int)yyVals[-6 + yyTop], (int)yyVals[-4 + yyTop], (int)yyVals[-2 + yyTop], (int)yyVals[0 + yyTop]);
                            }
                            break;
                        case 639:
#line 3255 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.CurrentAssemblyRef.SetPublicKey((byte[])yyVals[0 + yyTop]);
                            }
                            break;
                        case 640:
#line 3259 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.CurrentAssemblyRef.SetPublicKeyToken((byte[])yyVals[0 + yyTop]);
                            }
                            break;
                        case 641:
#line 3263 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.CurrentAssemblyRef.SetLocale((string)yyVals[0 + yyTop]);
                            }
                            break;
                        case 643:
#line 3269 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.CurrentAssemblyRef.SetHash((byte[])yyVals[0 + yyTop]);
                            }
                            break;
                        case 644:
                            case_644();
                            break;
                        case 647:
#line 3284 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                current_extern = new KeyValuePair<string, TypeAttr>((string)yyVals[0 + yyTop], (TypeAttr)yyVals[-1 + yyTop]);
                            }
                            break;
                        case 648:
#line 3287 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = 0; }
                            break;
                        case 649:
#line 3288 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-1 + yyTop] | TypeAttr.Private; }
                            break;
                        case 650:
#line 3289 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-1 + yyTop] | TypeAttr.Public; }
                            break;
                        case 651:
#line 3290 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-1 + yyTop] | TypeAttr.Forwarder; }
                            break;
                        case 652:
#line 3291 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-2 + yyTop] | TypeAttr.NestedPublic; }
                            break;
                        case 653:
#line 3292 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-2 + yyTop] | TypeAttr.NestedPrivate; }
                            break;
                        case 654:
#line 3293 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-2 + yyTop] | TypeAttr.NestedFamily; }
                            break;
                        case 655:
#line 3294 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-2 + yyTop] | TypeAttr.NestedAssembly; }
                            break;
                        case 656:
#line 3295 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-2 + yyTop] | TypeAttr.NestedFamAndAssem; }
                            break;
                        case 657:
#line 3296 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = (TypeAttr)yyVals[-2 + yyTop] | TypeAttr.NestedFamOrAssem; }
                            break;
                        case 663:
#line 3309 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                codegen.ExternTable.AddClass(current_extern.Key, current_extern.Value, (string)yyVals[0 + yyTop]);
                            }
                            break;
                        case 665:
                            case_665();
                            break;
                        case 667:
#line 3327 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = ManifestResource.PublicResource; }
                            break;
                        case 668:
#line 3328 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = ManifestResource.PrivateResource; }
                            break;
                        case 675:
#line 3341 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = String.Format("{0}{1}", yyVals[-2 + yyTop], yyVals[0 + yyTop]); }
                            break;
                        case 676:
                            case_676();
                            break;
                        case 679:
                            case_679();
                            break;
                        case 680:
                            case_680();
                            break;
                        case 681:
                            case_681();
                            break;
                        case 682:
                            case_682();
                            break;
                        case 683:
#line 3380 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { }
                            break;
                        case 684:
#line 3386 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                tokenizer.InByteArray = true;
                            }
                            break;
                        case 685:
                            case_685();
                            break;
                        case 686:
#line 3394 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            { yyVal = new byte[0]; }
                            break;
                        case 687:
                            case_687();
                            break;
                        case 688:
                            case_688();
                            break;
                        case 689:
                            case_689();
                            break;
                        case 690:
#line 3418 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = true;
                            }
                            break;
                        case 691:
#line 3422 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = false;
                            }
                            break;
                        case 695:
#line 3433 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
                            {
                                yyVal = (string)yyVals[-2 + yyTop] + '.' + (string)yyVals[0 + yyTop];
                            }
                            break;
#line default
                    }
                    yyTop -= yyLen[yyN];
                    yyState = yyStates[yyTop];
                    int yyM = yyLhs[yyN];
                    if (yyState == 0 && yyM == 0)
                    {
                        if (debug != null) debug.shift(0, yyFinal);
                        yyState = yyFinal;
                        if (yyToken < 0)
                        {
                            yyToken = yyLex.advance() ? yyLex.token() : 0;
                            if (debug != null)
                                debug.lex(yyState, yyToken, yyname(yyToken), yyLex.value());
                        }
                        if (yyToken == 0)
                        {
                            if (debug != null) debug.accept(yyVal);
                            return yyVal;
                        }
                        goto continue_yyLoop;
                    }
                    if (((yyN = yyGindex[yyM]) != 0) && ((yyN += yyState) >= 0)
                        && (yyN < yyTable.Length) && (yyCheck[yyN] == yyState))
                        yyState = yyTable[yyN];
                    else
                        yyState = yyDgoto[yyM];
                    if (debug != null) debug.shift(yyStates[yyTop], yyState);
                    goto continue_yyLoop;
                continue_yyDiscarded:;  // implements the named-loop continue: 'continue yyDiscarded'
                }
            continue_yyLoop:;       // implements the named-loop continue: 'continue yyLoop'
            }
        }

        /*
         All more than 3 lines long rules are wrapped into a method
        */
        void case_17()
#line 522 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            if (codegen.CurrentCustomAttrTarget != null)
                codegen.CurrentCustomAttrTarget.AddCustomAttribute((CustomAttr)yyVals[0 + yyTop]);
        }

        void case_41()
#line 590 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            codegen.BeginTypeDef((TypeAttr)yyVals[-4 + yyTop], (string)yyVals[-3 + yyTop],
    yyVals[-1 + yyTop] as BaseClassRef, yyVals[0 + yyTop] as ArrayList, null, (GenericParameters)yyVals[-2 + yyTop]);

            if (is_value_class)
                codegen.CurrentTypeDef.MakeValueClass();
            if (is_enum_class)
                codegen.CurrentTypeDef.MakeEnumClass();
        }

        void case_42()
#line 602 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            /* Reset some flags*/
            is_value_class = false;
            is_enum_class = false;
            yyVal = new TypeAttr();
        }

        void case_71()
#line 646 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList al = new ArrayList();
            al.Add(yyVals[0 + yyTop]);
            yyVal = al;
        }

        void case_72()
#line 652 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList al = (ArrayList)yyVals[-2 + yyTop];

            al.Add(yyVals[0 + yyTop]);
            yyVal = al;
        }

        void case_77()
#line 675 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            GenericArguments ga = new GenericArguments();
            ga.Add((BaseTypeRef)yyVals[0 + yyTop]);
            yyVal = ga;
        }

        void case_78()
#line 681 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ((GenericArguments)yyVals[-2 + yyTop]).Add((BaseTypeRef)yyVals[0 + yyTop]);
            yyVal = yyVals[-2 + yyTop];
        }

        void case_81()
#line 696 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList al = new ArrayList();
            al.Add(yyVals[0 + yyTop]);
            yyVal = al;
        }

        void case_82()
#line 702 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList al = (ArrayList)yyVals[-2 + yyTop];
            al.Add(yyVals[0 + yyTop]);
            yyVal = al;
        }

        void case_85()
#line 718 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            if (yyVals[0 + yyTop] != null)
                yyVal = ((BaseClassRef)yyVals[-1 + yyTop]).GetGenericTypeInst((GenericArguments)yyVals[0 + yyTop]);
            else
                yyVal = yyVals[-1 + yyTop];
        }

        void case_86()
#line 725 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            GenParam gpar = new GenParam((int)yyVals[0 + yyTop], "", GenParamType.Var);
            yyVal = new GenericParamRef(gpar, yyVals[0 + yyTop].ToString());
        }

        void case_87()
#line 730 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            GenParam gpar = new GenParam((int)yyVals[0 + yyTop], "", GenParamType.MVar);
            yyVal = new GenericParamRef(gpar, yyVals[0 + yyTop].ToString());
        }

        void case_88()
#line 735 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            int num = -1;
            string name = (string)yyVals[0 + yyTop];
            if (codegen.CurrentTypeDef != null)
                num = codegen.CurrentTypeDef.GetGenericParamNum(name);
            GenParam gpar = new GenParam(num, name, GenParamType.Var);
            yyVal = new GenericParamRef(gpar, name);
        }

        void case_89()
#line 744 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            int num = -1;
            string name = (string)yyVals[0 + yyTop];
            if (codegen.CurrentMethodDef != null)
                num = codegen.CurrentMethodDef.GetGenericParamNum(name);
            GenParam gpar = new GenParam(num, name, GenParamType.MVar);
            yyVal = new GenericParamRef(gpar, name);
        }

        void case_90()
#line 755 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            GenericParameter gp = new GenericParameter((string)yyVals[0 + yyTop], (PEAPI.GenericParamAttributes)yyVals[-2 + yyTop], (ArrayList)yyVals[-1 + yyTop]);

            GenericParameters colln = new GenericParameters();
            colln.Add(gp);
            yyVal = colln;
        }

        void case_91()
#line 763 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            GenericParameters colln = (GenericParameters)yyVals[-4 + yyTop];
            colln.Add(new GenericParameter((string)yyVals[0 + yyTop], (PEAPI.GenericParamAttributes)yyVals[-2 + yyTop], (ArrayList)yyVals[-1 + yyTop]));
            yyVal = colln;
        }

        void case_99()
#line 803 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            if (codegen.CurrentMethodDef != null)
                codegen.CurrentCustomAttrTarget = codegen.CurrentMethodDef.GetGenericParam((string)yyVals[0 + yyTop]);
            else
                codegen.CurrentCustomAttrTarget = codegen.CurrentTypeDef.GetGenericParam((string)yyVals[0 + yyTop]);
            if (codegen.CurrentCustomAttrTarget == null)
            {
                logger.Error(String.Format("Type parameter '{0}' undefined.", (string)yyVals[0 + yyTop]));
                FileProcessor.ErrorCount += 1;
            }    
        }

        void case_100()
#line 812 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            int index = ((int)yyVals[-1 + yyTop]);
            if (codegen.CurrentMethodDef != null)
                codegen.CurrentCustomAttrTarget = codegen.CurrentMethodDef.GetGenericParam(index - 1);
            else
                codegen.CurrentCustomAttrTarget = codegen.CurrentTypeDef.GetGenericParam(index - 1);
            if (codegen.CurrentCustomAttrTarget == null)
            {
                logger.Error(String.Format("Type parameter '{0}' index out of range.", index));
                FileProcessor.ErrorCount += 1;
            }
        }

        void case_101()
#line 824 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList class_list = new ArrayList();
            class_list.Add(yyVals[0 + yyTop]);
            yyVal = class_list;
        }

        void case_102()
#line 830 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList class_list = (ArrayList)yyVals[-2 + yyTop];
            class_list.Add(yyVals[0 + yyTop]);
        }

        void case_105()
#line 844 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            if (codegen.IsThisAssembly((string)yyVals[-2 + yyTop]))
            {
                yyVal = codegen.GetTypeRef((string)yyVals[0 + yyTop]);
            }
            else
            {
                yyVal = codegen.ExternTable.GetTypeRef((string)yyVals[-2 + yyTop], (string)yyVals[0 + yyTop], false);
            }
        }

        void case_106()
#line 852 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            if (codegen.IsThisModule((string)yyVals[-2 + yyTop]))
            {
                yyVal = codegen.GetTypeRef((string)yyVals[0 + yyTop]);
            }
            else
            {
                yyVal = codegen.ExternTable.GetModuleTypeRef((string)yyVals[-2 + yyTop], (string)yyVals[0 + yyTop], false);
            }
        }

        void case_107()
#line 860 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            PrimitiveTypeRef prim = PrimitiveTypeRef.GetPrimitiveType((string)yyVals[0 + yyTop]);

            if (prim != null && !codegen.IsThisAssembly("mscorlib"))
                yyVal = prim;
            else
                yyVal = codegen.GetTypeRef((string)yyVals[0 + yyTop]);
        }

        void case_118()
#line 887 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            if (codegen.CurrentCustomAttrTarget != null)
                codegen.CurrentCustomAttrTarget.AddCustomAttribute((CustomAttr)yyVals[0 + yyTop]);
        }

        void case_122()
#line 902 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            /**/
            /* My copy of the spec didn't have a type_list but*/
            /* it seems pretty crucial*/
            /**/
            BaseTypeRef owner = (BaseTypeRef)yyVals[-9 + yyTop];
            ArrayList arg_list = (ArrayList)yyVals[0 + yyTop];
            BaseTypeRef[] param_list;
            BaseMethodRef decl;

            if (arg_list != null)
                param_list = (BaseTypeRef[])arg_list.ToArray(typeof(BaseTypeRef));
            else
                param_list = new BaseTypeRef[0];

            decl = owner.GetMethodRef((BaseTypeRef)yyVals[-4 + yyTop],
                    (CallConv)yyVals[-5 + yyTop], (string)yyVals[-7 + yyTop], param_list, 0);

            /* NOTICE: `owner' here might be wrong*/
            string sig = MethodDef.CreateSignature(owner, (CallConv)yyVals[-5 + yyTop], (string)yyVals[-1 + yyTop],
                                                    param_list, 0, false);
            codegen.CurrentTypeDef.AddOverride(sig, decl);
        }

        void case_126()
#line 934 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            BaseClassRef class_ref = (BaseClassRef)yyVals[0 + yyTop];
            class_ref.MakeValueClass();
            yyVal = GetTypeRef(class_ref);
        }

        void case_127()
#line 940 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ExternTypeRef ext_ref = codegen.ExternTable.GetTypeRef((string)yyVals[-3 + yyTop], (string)yyVals[-1 + yyTop], true);
            if (yyVals[0 + yyTop] != null)
                yyVal = ext_ref.GetGenericTypeInst((GenericArguments)yyVals[0 + yyTop]);
            else
                yyVal = GetTypeRef(ext_ref);
        }

        void case_128()
#line 948 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            TypeRef t_ref = codegen.GetTypeRef((string)yyVals[-1 + yyTop]);
            t_ref.MakeValueClass();
            if (yyVals[0 + yyTop] != null)
                yyVal = t_ref.GetGenericTypeInst((GenericArguments)yyVals[0 + yyTop]);
            else
                yyVal = GetTypeRef(t_ref);
        }

        void case_129()
#line 957 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            BaseTypeRef base_type = (BaseTypeRef)yyVals[-2 + yyTop];
            base_type.MakeArray();
            yyVal = base_type;
        }

        void case_130()
#line 963 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            BaseTypeRef base_type = (BaseTypeRef)yyVals[-3 + yyTop];
            ArrayList bound_list = (ArrayList)yyVals[-1 + yyTop];
            base_type.MakeBoundArray(bound_list);
            yyVal = base_type;
        }

        void case_131()
#line 970 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            BaseTypeRef base_type = (BaseTypeRef)yyVals[-1 + yyTop];
            base_type.MakeManagedPointer();
            yyVal = base_type;
        }

        void case_132()
#line 976 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            BaseTypeRef base_type = (BaseTypeRef)yyVals[-1 + yyTop];
            base_type.MakeUnmanagedPointer();
            yyVal = base_type;
        }

        void case_133()
#line 982 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            BaseTypeRef base_type = (BaseTypeRef)yyVals[-1 + yyTop];
            base_type.MakePinned();
            yyVal = base_type;
        }

        void case_134()
#line 988 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            BaseTypeRef base_type = (BaseTypeRef)yyVals[-4 + yyTop];
            BaseTypeRef class_ref = (BaseTypeRef)yyVals[-1 + yyTop];
            base_type.MakeCustomModified(codegen,
                    CustomModifier.modreq, class_ref);
            yyVal = base_type;
        }

        void case_135()
#line 996 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            BaseTypeRef base_type = (BaseTypeRef)yyVals[-4 + yyTop];
            BaseTypeRef class_ref = (BaseTypeRef)yyVals[-1 + yyTop];
            base_type.MakeCustomModified(codegen,
                    CustomModifier.modopt, class_ref);
            yyVal = base_type;
        }

        void case_152()
#line 1067 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            /* TODO: Is this the proper full name*/
            yyVal = new PrimitiveTypeRef(PrimitiveType.NativeInt, "System.IntPtr");
        }

        void case_155()
#line 1080 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            yyVal = new PrimitiveTypeRef(PrimitiveType.TypedRef,
                    "System.TypedReference");
        }

        void case_161()
#line 1107 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList bound_list = new ArrayList();
            bound_list.Add(yyVals[0 + yyTop]);
            yyVal = bound_list;
        }

        void case_162()
#line 1113 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList bound_list = (ArrayList)yyVals[-2 + yyTop];
            bound_list.Add(yyVals[0 + yyTop]);
        }

        void case_163()
#line 1120 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            /* This is shortref for no lowerbound or size*/
            yyVal = new DictionaryEntry(TypeRef.Ellipsis, TypeRef.Ellipsis);
        }

        void case_164()
#line 1125 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            /* No lower bound or size*/
            yyVal = new DictionaryEntry(TypeRef.Ellipsis, TypeRef.Ellipsis);
        }

        void case_165()
#line 1130 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            /* Only size specified */
            int size = (int)yyVals[0 + yyTop];
            if (size < 0)
                /* size cannot be < 0, so emit as (0, ...)
                   ilasm.net emits it like this */
                yyVal = new DictionaryEntry(0, TypeRef.Ellipsis);
            else
                yyVal = new DictionaryEntry(TypeRef.Ellipsis, size);
        }

        void case_166()
#line 1141 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            /* lower and upper bound*/
            int lower = (int)yyVals[-2 + yyTop];
            int upper = (int)yyVals[0 + yyTop];
            if (lower > upper)
            {
                logger.Error("Lower bound " + lower + " must be <= upper bound " + upper);
                FileProcessor.ErrorCount += 1;
            }

            yyVal = new DictionaryEntry(yyVals[-2 + yyTop], yyVals[0 + yyTop]);
        }

        void case_167()
#line 1151 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            /* Just lower bound*/
            yyVal = new DictionaryEntry(yyVals[-1 + yyTop], TypeRef.Ellipsis);
        }

        void case_205()
#line 1295 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            /*FIXME: Allowed only for methods, !fields*/
            yyVal = new NativeArray((NativeType)yyVals[-5 + yyTop], (int)yyVals[-3 + yyTop], (int)yyVals[-1 + yyTop]);
        }

        void case_206()
#line 1300 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            /*FIXME: Allowed only for methods, !fields*/
            yyVal = new NativeArray((NativeType)yyVals[-4 + yyTop], -1, (int)yyVals[-1 + yyTop]);
        }

        void case_219()
#line 1344 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            if (yyVals[0 + yyTop] == null)
                yyVal = new SafeArray();
            else
                yyVal = new SafeArray((SafeArrayType)yyVals[0 + yyTop]);
        }

        void case_277()
#line 1499 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            FieldDef field_def = new FieldDef((FieldAttr)yyVals[-5 + yyTop],
(string)yyVals[-3 + yyTop], (BaseTypeRef)yyVals[-4 + yyTop]);
            codegen.AddFieldDef(field_def);
            codegen.CurrentCustomAttrTarget = field_def;

            if (yyVals[-6 + yyTop] != null)
            {
                field_def.SetOffset((uint)(int)yyVals[-6 + yyTop]);
            }

            if (yyVals[-2 + yyTop] != null)
            {
                field_def.AddDataValue((string)yyVals[-2 + yyTop]);
            }

            if (yyVals[-1 + yyTop] != null)
            {
                field_def.SetValue((Constant)yyVals[-1 + yyTop]);
            }
        }

        void case_292()
#line 1575 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            codegen.AddFieldMarshalInfo((NativeType)yyVals[-1 + yyTop]);
            yyVal = (FieldAttr)yyVals[-4 + yyTop] | FieldAttr.HasFieldMarshal;
        }

        void case_317()
#line 1674 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            /* ******** THIS IS NOT IN THE DOCUMENTATION ******** //*/
            yyVal = new StringConst((string)yyVals[0 + yyTop]);
        }

        void case_323()
#line 1701 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            var l = (List<BoolConst>)yyVals[-1 + yyTop];
            l.Add(new BoolConst((bool)yyVals[0 + yyTop]));
            yyVal = l;
        }

        void case_324()
#line 1709 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            DataDef datadef = (DataDef)yyVals[-1 + yyTop];

            if (yyVals[0 + yyTop] is ArrayList)
            {
                ArrayList const_list = (ArrayList)yyVals[0 + yyTop];
                DataConstant[] const_arr = new DataConstant[const_list.Count];

                for (int i = 0; i < const_arr.Length; i++)
                    const_arr[i] = (DataConstant)const_list[i];

                datadef.PeapiConstant = new ArrayConstant(const_arr);
            }
            else
            {
                datadef.PeapiConstant = (PEAPI.Constant)yyVals[0 + yyTop];
            }
            codegen.AddDataDef(datadef);
        }

        void case_332()
#line 1750 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList dataitem_list = new ArrayList();
            dataitem_list.Add(yyVals[0 + yyTop]);
            yyVal = dataitem_list;
        }

        void case_333()
#line 1756 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList list = (ArrayList)yyVals[-2 + yyTop];
            list.Add(yyVals[0 + yyTop]);
        }

        void case_336()
#line 1771 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            /*     DataDef def = codegen.CurrentTypeDef.GetDataDef ((string) $3);*/
            /*     $$ = new AddressConstant ((DataConstant) def.PeapiConstant);*/
        }

        void case_338()
#line 1780 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            /* ******** THIS IS NOT IN THE SPECIFICATION ******** //*/
            yyVal = new ByteArrConst((byte[])yyVals[0 + yyTop]);
        }

        void case_339()
#line 1785 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            double d = (double)yyVals[-2 + yyTop];
            FloatConst float_const = new FloatConst((float)d);

            if (yyVals[0 + yyTop] != null)
                yyVal = new RepeatedConstant(float_const, (int)yyVals[0 + yyTop]);
            else
                yyVal = float_const;
        }

        void case_340()
#line 1795 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            DoubleConst double_const = new DoubleConst((double)yyVals[-2 + yyTop]);

            if (yyVals[0 + yyTop] != null)
                yyVal = new RepeatedConstant(double_const, (int)yyVals[0 + yyTop]);
            else
                yyVal = double_const;
        }

        void case_341()
#line 1804 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            IntConst int_const = new IntConst((long)yyVals[-2 + yyTop]);

            if (yyVals[0 + yyTop] != null)
                yyVal = new RepeatedConstant(int_const, (int)yyVals[0 + yyTop]);
            else
                yyVal = int_const;
        }

        void case_342()
#line 1813 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            IntConst int_const = new IntConst((int)yyVals[-2 + yyTop]);

            if (yyVals[0 + yyTop] != null)
                yyVal = new RepeatedConstant(int_const, (int)yyVals[0 + yyTop]);
            else
                yyVal = int_const;
        }

        void case_343()
#line 1822 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            int i = (int)yyVals[-2 + yyTop];
            IntConst int_const = new IntConst((short)i);

            if (yyVals[0 + yyTop] != null)
                yyVal = new RepeatedConstant(int_const, (int)yyVals[0 + yyTop]);
            else
                yyVal = int_const;
        }

        void case_344()
#line 1832 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            int i = (int)yyVals[-2 + yyTop];
            IntConst int_const = new IntConst((sbyte)i);

            if (yyVals[0 + yyTop] != null)
                yyVal = new RepeatedConstant(int_const, (int)yyVals[0 + yyTop]);
            else
                yyVal = int_const;
        }

        void case_345()
#line 1842 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            FloatConst float_const = new FloatConst(0F);

            if (yyVals[0 + yyTop] != null)
                yyVal = new RepeatedConstant(float_const, (int)yyVals[0 + yyTop]);
            else
                yyVal = float_const;
        }

        void case_346()
#line 1851 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            DoubleConst double_const = new DoubleConst(0);

            if (yyVals[0 + yyTop] != null)
                yyVal = new RepeatedConstant(double_const, (int)yyVals[0 + yyTop]);
            else
                yyVal = double_const;
        }

        void case_347()
#line 1860 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            IntConst int_const = new IntConst((long)0);

            if (yyVals[0 + yyTop] != null)
                yyVal = new RepeatedConstant(int_const, (int)yyVals[0 + yyTop]);
            else
                yyVal = int_const;
        }

        void case_348()
#line 1869 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            IntConst int_const = new IntConst((int)0);

            if (yyVals[0 + yyTop] != null)
                yyVal = new RepeatedConstant(int_const, (int)yyVals[0 + yyTop]);
            else
                yyVal = int_const;
        }

        void case_349()
#line 1878 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            IntConst int_const = new IntConst((short)0);

            if (yyVals[0 + yyTop] != null)
                yyVal = new RepeatedConstant(int_const, (int)yyVals[0 + yyTop]);
            else
                yyVal = int_const;
        }

        void case_350()
#line 1887 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            IntConst int_const = new IntConst((sbyte)0);

            if (yyVals[0 + yyTop] != null)
                yyVal = new RepeatedConstant(int_const, (int)yyVals[0 + yyTop]);
            else
                yyVal = int_const;
        }

        void case_352()
#line 1905 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            CallConv cc = (CallConv)yyVals[-8 + yyTop];
            if (yyVals[-4 + yyTop] != null)
                cc |= CallConv.Generic;

            MethodDef methdef = new MethodDef(
                    codegen, (MethAttr)yyVals[-9 + yyTop], cc,
                    (ImplAttr)yyVals[0 + yyTop], (string)yyVals[-5 + yyTop], (BaseTypeRef)yyVals[-6 + yyTop],
                    (ArrayList)yyVals[-2 + yyTop], tokenizer.Reader.Location, (GenericParameters)yyVals[-4 + yyTop], codegen.CurrentTypeDef);
            if (pinvoke_info)
            {
                ExternModule mod = codegen.ExternTable.AddModule(pinvoke_mod);
                methdef.AddPInvokeInfo(pinvoke_attr, mod, pinvoke_meth);
                pinvoke_info = false;
            }
        }

        void case_353()
#line 1923 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            MethodDef methdef = new MethodDef(
                  codegen, (MethAttr)yyVals[-12 + yyTop], (CallConv)yyVals[-11 + yyTop],
                    (ImplAttr)yyVals[0 + yyTop], (string)yyVals[-4 + yyTop], (BaseTypeRef)yyVals[-9 + yyTop],
                    (ArrayList)yyVals[-2 + yyTop], tokenizer.Reader.Location, null, codegen.CurrentTypeDef);

            if (pinvoke_info)
            {
                ExternModule mod = codegen.ExternTable.AddModule(pinvoke_mod);
                methdef.AddPInvokeInfo(pinvoke_attr, mod, pinvoke_meth);
                pinvoke_info = false;
            }

            methdef.AddRetTypeMarshalInfo((NativeType)yyVals[-6 + yyTop]);
        }

        void case_374()
#line 1961 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            pinvoke_info = true;
            pinvoke_mod = (string)yyVals[-4 + yyTop];
            pinvoke_meth = (string)yyVals[-2 + yyTop];
            pinvoke_attr = (PInvokeAttr)yyVals[-1 + yyTop];
        }

        void case_375()
#line 1968 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            pinvoke_info = true;
            pinvoke_mod = (string)yyVals[-2 + yyTop];
            pinvoke_meth = null;
            pinvoke_attr = (PInvokeAttr)yyVals[-1 + yyTop];
        }

        void case_376()
#line 1975 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            pinvoke_info = true;
            pinvoke_mod = null;
            pinvoke_meth = null;
            pinvoke_attr = (PInvokeAttr)yyVals[-1 + yyTop];
        }

        void case_416()
#line 2033 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList sig_list = new ArrayList();
            sig_list.Add(yyVals[0 + yyTop]);
            yyVal = sig_list;
        }

        void case_417()
#line 2039 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList sig_list = (ArrayList)yyVals[-2 + yyTop];
            sig_list.Add(yyVals[0 + yyTop]);
            yyVal = sig_list;
        }

        void case_420()
#line 2055 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            yyVal = new ParamDef((ParamAttr)0, "...", new SentinelTypeRef());
            /* $$ = ParamDef.Ellipsis;*/
        }

        void case_421()
#line 2060 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ParamDef param_def = new ParamDef((ParamAttr)yyVals[-5 + yyTop], null, (BaseTypeRef)yyVals[-4 + yyTop]);
            param_def.AddMarshalInfo((PEAPI.NativeType)yyVals[-1 + yyTop]);

            yyVal = param_def;
        }

        void case_422()
#line 2067 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ParamDef param_def = new ParamDef((ParamAttr)yyVals[-6 + yyTop], (string)yyVals[0 + yyTop], (BaseTypeRef)yyVals[-5 + yyTop]);
            param_def.AddMarshalInfo((PEAPI.NativeType)yyVals[-2 + yyTop]);

            yyVal = param_def;
        }

        void case_424()
#line 2080 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList type_list = new ArrayList();
            /* type_list.Add (TypeRef.Ellipsis);*/
            type_list.Add(new SentinelTypeRef());
            yyVal = type_list;
        }

        void case_425()
#line 2087 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList type_list = (ArrayList)yyVals[-2 + yyTop];
            /* type_list.Add (TypeRef.Ellipsis);*/
            type_list.Add(new SentinelTypeRef());
            yyVal = type_list;
        }

        void case_426()
#line 2094 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList type_list = new ArrayList();
            type_list.Add(yyVals[-1 + yyTop]);
            yyVal = type_list;
        }

        void case_427()
#line 2100 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList type_list = (ArrayList)yyVals[-4 + yyTop];
            type_list.Add(yyVals[-1 + yyTop]);
        }

        void case_432()
#line 2115 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            codegen.CurrentMethodDef.AddInstr(new
                        EmitByteInstr((int)yyVals[0 + yyTop], tokenizer.Location));

        }

        void case_434()
#line 2125 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            if (yyVals[-1 + yyTop] != null)
            {
                codegen.CurrentMethodDef.AddLocals(
                        (ArrayList)yyVals[-1 + yyTop]);
            }
        }

        void case_435()
#line 2132 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            if (yyVals[-1 + yyTop] != null)
            {
                codegen.CurrentMethodDef.AddLocals(
                        (ArrayList)yyVals[-1 + yyTop]);
                codegen.CurrentMethodDef.InitLocals();
            }
        }

        void case_436()
#line 2140 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            codegen.CurrentMethodDef.EntryPoint();
            codegen.HasEntryPoint = true;
        }

        void case_441()
#line 2152 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            codegen.CurrentTypeDef.AddOverride(codegen.CurrentMethodDef,
                    (BaseTypeRef)yyVals[-2 + yyTop], (string)yyVals[0 + yyTop]);

        }

        void case_442()
#line 2158 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            codegen.CurrentTypeDef.AddOverride(codegen.CurrentMethodDef.Signature,
                (BaseMethodRef)yyVals[0 + yyTop]);
        }

        void case_443()
#line 2165 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            BaseTypeRef owner = (BaseTypeRef)yyVals[-10 + yyTop];
            ArrayList arg_list = (ArrayList)yyVals[-1 + yyTop];
            BaseTypeRef[] param_list;
            BaseMethodRef methref;

            if (arg_list != null)
                param_list = (BaseTypeRef[])arg_list.ToArray(typeof(BaseTypeRef));
            else
                param_list = new BaseTypeRef[0];

            if (owner.UseTypeSpec)
            {
                methref = new TypeSpecMethodRef(owner, (CallConv)yyVals[-12 + yyTop], (BaseTypeRef)yyVals[-11 + yyTop],
                        (string)yyVals[-8 + yyTop], param_list, (int)yyVals[-5 + yyTop]);
            }
            else
            {
                methref = owner.GetMethodRef((BaseTypeRef)yyVals[-11 + yyTop],
                        (CallConv)yyVals[-12 + yyTop], (string)yyVals[-8 + yyTop], param_list, (int)yyVals[-5 + yyTop]);
            }

            codegen.CurrentTypeDef.AddOverride(codegen.CurrentMethodDef.Signature,
                methref);
        }

        void case_445()
#line 2189 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            int index = (int)yyVals[-2 + yyTop];
            ParamDef param = codegen.CurrentMethodDef.GetParam(index);
            codegen.CurrentCustomAttrTarget = param;

            if (param == null)
            {
                logger.Warning(tokenizer.Location, String.Format("invalid param index ({0}) with .param", index));
            }
            else if (yyVals[0 + yyTop] != null)
                param.AddDefaultValue((Constant)yyVals[0 + yyTop]);
        }

        void case_453()
#line 2213 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            if (codegen.CurrentCustomAttrTarget != null)
                codegen.CurrentCustomAttrTarget.AddCustomAttribute((CustomAttr)yyVals[0 + yyTop]);
        }

        void case_456()
#line 2222 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList local_list = new ArrayList();
            local_list.Add(yyVals[0 + yyTop]);
            yyVal = local_list;
        }

        void case_457()
#line 2228 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList local_list = (ArrayList)yyVals[-2 + yyTop];
            local_list.Add(yyVals[0 + yyTop]);
        }

        void case_463()
#line 2259 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            /* This is a reference to a global method in another*/
            /* assembly. This is not supported in the MS version of ilasm*/
        }

        void case_464()
#line 2264 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            string module = (string)yyVals[-1 + yyTop];

            if (codegen.IsThisModule(module))
            {
                /* This is not handled yet.*/
            }
            else
            {
                yyVal = codegen.ExternTable.GetModuleTypeRef((string)yyVals[-1 + yyTop], "<Module>", false);
            }

        }

        void case_466()
#line 2278 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            yyVal = new HandlerBlock((LabelInfo)yyVals[-2 + yyTop],
                    codegen.CurrentMethodDef.AddLabel());
            codegen.CurrentMethodDef.EndLocalsScope();
        }

        void case_467()
#line 2286 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            yyVal = codegen.CurrentMethodDef.AddLabel();
            codegen.CurrentMethodDef.BeginLocalsScope();
        }

        void case_468()
#line 2294 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            TryBlock try_block = (TryBlock)yyVals[-1 + yyTop];

            ArrayList clause_list = (ArrayList)yyVals[0 + yyTop];
            foreach (object clause in clause_list)
                try_block.AddSehClause((ISehClause)clause);

            codegen.CurrentMethodDef.AddInstr(try_block);
        }

        void case_470()
#line 2310 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            LabelInfo from = codegen.CurrentMethodDef.AddLabelRef((string)yyVals[-2 + yyTop]);
            LabelInfo to = codegen.CurrentMethodDef.AddLabelRef((string)yyVals[0 + yyTop]);

            yyVal = new TryBlock(new HandlerBlock(from, to), tokenizer.Location);
        }

        void case_471()
#line 2317 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            LabelInfo from = codegen.CurrentMethodDef.AddLabel((int)yyVals[-2 + yyTop]);
            LabelInfo to = codegen.CurrentMethodDef.AddLabel((int)yyVals[0 + yyTop]);

            yyVal = new TryBlock(new HandlerBlock(from, to), tokenizer.Location);
        }

        void case_472()
#line 2326 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList clause_list = new ArrayList();
            clause_list.Add(yyVals[0 + yyTop]);
            yyVal = clause_list;
        }

        void case_473()
#line 2332 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList clause_list = (ArrayList)yyVals[-1 + yyTop];
            clause_list.Add(yyVals[0 + yyTop]);
        }

        void case_474()
#line 2339 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            if (yyVals[-1 + yyTop].GetType() == typeof(PrimitiveTypeRef))
            {
                logger.Error("Exception not be of a primitive type.");
                FileProcessor.ErrorCount += 1;
            }

            BaseTypeRef type = (BaseTypeRef)yyVals[-1 + yyTop];
            CatchBlock cb = new CatchBlock(type);
            cb.SetHandlerBlock((HandlerBlock)yyVals[0 + yyTop]);
            yyVal = cb;
        }

        void case_475()
#line 2349 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            FinallyBlock fb = new FinallyBlock();
            fb.SetHandlerBlock((HandlerBlock)yyVals[0 + yyTop]);
            yyVal = fb;
        }

        void case_476()
#line 2355 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            FaultBlock fb = new FaultBlock();
            fb.SetHandlerBlock((HandlerBlock)yyVals[0 + yyTop]);
            yyVal = fb;
        }

        void case_477()
#line 2361 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            FilterBlock fb = (FilterBlock)yyVals[-1 + yyTop];
            fb.SetHandlerBlock((HandlerBlock)yyVals[0 + yyTop]);
        }

        void case_478()
#line 2368 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            HandlerBlock block = (HandlerBlock)yyVals[0 + yyTop];
            FilterBlock fb = new FilterBlock(block);
            yyVal = fb;
        }

        void case_479()
#line 2374 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            LabelInfo from = codegen.CurrentMethodDef.AddLabelRef((string)yyVals[0 + yyTop]);
            FilterBlock fb = new FilterBlock(new HandlerBlock(from, null));
            yyVal = fb;
        }

        void case_480()
#line 2380 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            LabelInfo from = codegen.CurrentMethodDef.AddLabel((int)yyVals[0 + yyTop]);
            FilterBlock fb = new FilterBlock(new HandlerBlock(from, null));
            yyVal = fb;
        }

        void case_482()
#line 2392 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            LabelInfo from = codegen.CurrentMethodDef.AddLabelRef((string)yyVals[-2 + yyTop]);
            LabelInfo to = codegen.CurrentMethodDef.AddLabelRef((string)yyVals[0 + yyTop]);

            yyVal = new HandlerBlock(from, to);
        }

        void case_483()
#line 2399 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            LabelInfo from = codegen.CurrentMethodDef.AddLabel((int)yyVals[-2 + yyTop]);
            LabelInfo to = codegen.CurrentMethodDef.AddLabel((int)yyVals[0 + yyTop]);

            yyVal = new HandlerBlock(from, to);
        }

        void case_484()
#line 2408 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            codegen.CurrentMethodDef.AddInstr(
                    new SimpInstr((Op)yyVals[0 + yyTop], tokenizer.Location));
        }

        void case_485()
#line 2413 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            codegen.CurrentMethodDef.AddInstr(
                    new IntInstr((IntOp)yyVals[-1 + yyTop], (int)yyVals[0 + yyTop], tokenizer.Location));
        }

        void case_486()
#line 2418 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            int slot = codegen.CurrentMethodDef.GetNamedLocalSlot((string)yyVals[0 + yyTop]);
            if (slot < 0)
            {
                logger.Error(String.Format("Undeclared identifier '{0}'", (string)yyVals[0 + yyTop]));
                FileProcessor.ErrorCount += 1;
            }
            codegen.CurrentMethodDef.AddInstr(
                    new IntInstr((IntOp)yyVals[-1 + yyTop], slot, tokenizer.Location));
        }

        void case_487()
#line 2426 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            codegen.CurrentMethodDef.AddInstr(
                    new IntInstr((IntOp)yyVals[-1 + yyTop], (int)yyVals[0 + yyTop], tokenizer.Location));
        }

        void case_488()
#line 2431 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            int pos = codegen.CurrentMethodDef.GetNamedParamPos((string)yyVals[0 + yyTop]);
            if (pos < 0)
            {
                logger.Error(String.Format("Undeclared identifier '{0}'", (string)yyVals[0 + yyTop]));
                FileProcessor.ErrorCount += 1;
            }

            codegen.CurrentMethodDef.AddInstr(
                    new IntInstr((IntOp)yyVals[-1 + yyTop], pos, tokenizer.Location));
        }

        void case_489()
#line 2440 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            codegen.CurrentMethodDef.AddInstr(new
                    IntInstr((IntOp)yyVals[-1 + yyTop], (int)yyVals[0 + yyTop], tokenizer.Location));
        }

        void case_490()
#line 2445 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            int slot = codegen.CurrentMethodDef.GetNamedLocalSlot((string)yyVals[0 + yyTop]);
            if (slot < 0)
            {
                logger.Error(String.Format("Undeclared identifier '{0}'", (string)yyVals[0 + yyTop]));
                FileProcessor.ErrorCount += 1;
            }
            codegen.CurrentMethodDef.AddInstr(new
                    IntInstr((IntOp)yyVals[-1 + yyTop], slot, tokenizer.Location));
        }

        void case_491()
#line 2453 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            if (yyVals[-1 + yyTop] is MiscInstr)
            {
                switch ((MiscInstr)yyVals[-1 + yyTop])
                {
                    case MiscInstr.ldc_i8:
                        codegen.CurrentMethodDef.AddInstr(new LdcInstr((MiscInstr)yyVals[-1 + yyTop],
                                (long)yyVals[0 + yyTop], tokenizer.Location));
                        break;
                }
            }
        }

        void case_492()
#line 2464 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            switch ((MiscInstr)yyVals[-1 + yyTop])
            {
                case MiscInstr.ldc_r4:
                case MiscInstr.ldc_r8:
                    codegen.CurrentMethodDef.AddInstr(new LdcInstr((MiscInstr)yyVals[-1 + yyTop], (double)yyVals[0 + yyTop], tokenizer.Location));
                    break;
            }
        }

        void case_493()
#line 2473 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            long l = (long)yyVals[0 + yyTop];

            switch ((MiscInstr)yyVals[-1 + yyTop])
            {
                case MiscInstr.ldc_r4:
                case MiscInstr.ldc_r8:
                    codegen.CurrentMethodDef.AddInstr(new LdcInstr((MiscInstr)yyVals[-1 + yyTop], (double)l, tokenizer.Location));
                    break;
            }
        }

        void case_494()
#line 2484 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            byte[] fpdata;
            switch ((MiscInstr)yyVals[-1 + yyTop])
            {
                case MiscInstr.ldc_r4:
                    fpdata = (byte[])yyVals[0 + yyTop];
                    if (!BitConverter.IsLittleEndian)
                    {
                        System.Array.Reverse(fpdata, 0, 4);
                    }
                    float s = BitConverter.ToSingle(fpdata, 0);
                    codegen.CurrentMethodDef.AddInstr(new LdcInstr((MiscInstr)yyVals[-1 + yyTop], s, tokenizer.Location));
                    break;
                case MiscInstr.ldc_r8:
                    fpdata = (byte[])yyVals[0 + yyTop];
                    if (!BitConverter.IsLittleEndian)
                    {
                        System.Array.Reverse(fpdata, 0, 8);
                    }
                    double d = BitConverter.ToDouble(fpdata, 0);
                    codegen.CurrentMethodDef.AddInstr(new LdcInstr((MiscInstr)yyVals[-1 + yyTop], d, tokenizer.Location));
                    break;
            }
        }

        void case_495()
#line 2506 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            LabelInfo target = codegen.CurrentMethodDef.AddLabel((int)yyVals[0 + yyTop]);
            codegen.CurrentMethodDef.AddInstr(new BranchInstr((BranchOp)yyVals[-1 + yyTop],
               target, tokenizer.Location));
        }

        void case_496()
#line 2512 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            LabelInfo target = codegen.CurrentMethodDef.AddLabelRef((string)yyVals[0 + yyTop]);
            codegen.CurrentMethodDef.AddInstr(new BranchInstr((BranchOp)yyVals[-1 + yyTop],
                                   target, tokenizer.Location));
        }

        void case_497()
#line 2518 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            codegen.CurrentMethodDef.AddInstr(new MethodInstr((MethodOp)yyVals[-1 + yyTop],
                    (BaseMethodRef)yyVals[0 + yyTop], tokenizer.Location));
        }

        void case_498()
#line 2523 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {

            BaseTypeRef owner = (BaseTypeRef)yyVals[-2 + yyTop];
            GenericParamRef gpr = yyVals[-3 + yyTop] as GenericParamRef;
            if (gpr != null && codegen.CurrentMethodDef != null)
                codegen.CurrentMethodDef.ResolveGenParam((PEAPI.GenParam)gpr.PeapiType);
            IFieldRef fieldref = owner.GetFieldRef(
                    (BaseTypeRef)yyVals[-3 + yyTop], (string)yyVals[0 + yyTop]);

            codegen.CurrentMethodDef.AddInstr(new FieldInstr((FieldOp)yyVals[-4 + yyTop], fieldref, tokenizer.Location));
        }

        void case_499()
#line 2535 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            GlobalFieldRef fieldref = codegen.GetGlobalFieldRef((BaseTypeRef)yyVals[-1 + yyTop], (string)yyVals[0 + yyTop]);

            codegen.CurrentMethodDef.AddInstr(new FieldInstr((FieldOp)yyVals[-2 + yyTop], fieldref, tokenizer.Location));
        }

        void case_500()
#line 2541 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            codegen.CurrentMethodDef.AddInstr(new TypeInstr((TypeOp)yyVals[-1 + yyTop],
                    (BaseTypeRef)yyVals[0 + yyTop], tokenizer.Location));
        }

        void case_501()
#line 2546 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            if ((MiscInstr)yyVals[-1 + yyTop] == MiscInstr.ldstr)
                codegen.CurrentMethodDef.AddInstr(new LdstrInstr((string)yyVals[0 + yyTop], tokenizer.Location));
        }

        void case_502()
#line 2551 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            byte[] bs = (byte[])yyVals[0 + yyTop];
            if ((MiscInstr)yyVals[-3 + yyTop] == MiscInstr.ldstr)
                codegen.CurrentMethodDef.AddInstr(new LdstrInstr(bs, tokenizer.Location));
        }

        void case_503()
#line 2557 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            byte[] bs = (byte[])yyVals[0 + yyTop];
            if ((MiscInstr)yyVals[-2 + yyTop] == MiscInstr.ldstr)
                codegen.CurrentMethodDef.AddInstr(new LdstrInstr(bs, tokenizer.Location));
        }

        void case_504()
#line 2563 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList arg_list = (ArrayList)yyVals[-1 + yyTop];
            BaseTypeRef[] arg_array = null;

            if (arg_list != null)
                arg_array = (BaseTypeRef[])arg_list.ToArray(typeof(BaseTypeRef));

            codegen.CurrentMethodDef.AddInstr(new CalliInstr((CallConv)yyVals[-4 + yyTop],
                    (BaseTypeRef)yyVals[-3 + yyTop], arg_array, tokenizer.Location));
        }

        void case_505()
#line 2574 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            if ((MiscInstr)yyVals[-1 + yyTop] == MiscInstr.ldtoken)
            {
                if (yyVals[0 + yyTop] is BaseMethodRef)
                    codegen.CurrentMethodDef.AddInstr(new LdtokenInstr((BaseMethodRef)yyVals[0 + yyTop], tokenizer.Location));
                else if (yyVals[0 + yyTop] is IFieldRef)
                    codegen.CurrentMethodDef.AddInstr(new LdtokenInstr((IFieldRef)yyVals[0 + yyTop], tokenizer.Location));
                else
                    codegen.CurrentMethodDef.AddInstr(new LdtokenInstr((BaseTypeRef)yyVals[0 + yyTop], tokenizer.Location));

            }
        }

        void case_507()
#line 2593 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList arg_list = (ArrayList)yyVals[-1 + yyTop];
            GenericArguments ga = (GenericArguments)yyVals[-3 + yyTop];
            BaseTypeRef[] param_list;

            if (arg_list != null)
                param_list = (BaseTypeRef[])arg_list.ToArray(typeof(BaseTypeRef));
            else
                param_list = new BaseTypeRef[0];

            BaseMethodRef methref = codegen.GetGlobalMethodRef((BaseTypeRef)yyVals[-5 + yyTop], (CallConv)yyVals[-6 + yyTop],
                                    (string)yyVals[-4 + yyTop], param_list, (ga != null ? ga.Count : 0));

            if (ga != null)
                methref = methref.GetGenericMethodRef(ga);

            yyVal = methref;
        }

        void case_508()
#line 2613 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            BaseTypeRef owner = (BaseTypeRef)yyVals[-6 + yyTop];
            ArrayList arg_list = (ArrayList)yyVals[-1 + yyTop];
            GenericArguments ga = (GenericArguments)yyVals[-3 + yyTop];
            BaseTypeRef[] param_list;
            BaseMethodRef methref;

            if (arg_list != null)
                param_list = (BaseTypeRef[])arg_list.ToArray(typeof(BaseTypeRef));
            else
                param_list = new BaseTypeRef[0];

            if (codegen.IsThisAssembly("mscorlib"))
            {
                PrimitiveTypeRef prim = owner as PrimitiveTypeRef;
                if (prim != null && prim.SigMod == "")
                    owner = codegen.GetTypeRef(prim.Name);
            }

            if (owner.UseTypeSpec)
            {
                methref = new TypeSpecMethodRef(owner, (CallConv)yyVals[-8 + yyTop], (BaseTypeRef)yyVals[-7 + yyTop],
                        (string)yyVals[-4 + yyTop], param_list, (ga != null ? ga.Count : 0));
            }
            else
            {
                methref = owner.GetMethodRef((BaseTypeRef)yyVals[-7 + yyTop],
                        (CallConv)yyVals[-8 + yyTop], (string)yyVals[-4 + yyTop], param_list, (ga != null ? ga.Count : 0));
            }

            if (ga != null)
                methref = methref.GetGenericMethodRef(ga);

            yyVal = methref;
        }

        void case_510()
#line 2648 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList label_list = new ArrayList();
            label_list.Add(codegen.CurrentMethodDef.AddLabelRef((string)yyVals[0 + yyTop]));
            yyVal = label_list;
        }

        void case_511()
#line 2654 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList label_list = new ArrayList();
            label_list.Add(yyVals[0 + yyTop]);
            yyVal = label_list;
        }

        void case_512()
#line 2660 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList label_list = (ArrayList)yyVals[-2 + yyTop];
            label_list.Add(codegen.CurrentMethodDef.AddLabelRef((string)yyVals[0 + yyTop]));
        }

        void case_513()
#line 2665 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList label_list = (ArrayList)yyVals[-2 + yyTop];
            label_list.Add(yyVals[0 + yyTop]);
        }

        void case_517()
#line 2680 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            BaseTypeRef owner = (BaseTypeRef)yyVals[-2 + yyTop];

            yyVal = owner.GetFieldRef(
                    (BaseTypeRef)yyVals[-3 + yyTop], (string)yyVals[0 + yyTop]);
        }

        void case_520()
#line 2699 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            EventDef event_def = new EventDef((FeatureAttr)yyVals[-2 + yyTop],
                    (BaseTypeRef)yyVals[-1 + yyTop], (string)yyVals[0 + yyTop]);
            codegen.CurrentTypeDef.BeginEventDef(event_def);
            codegen.CurrentCustomAttrTarget = event_def;
        }

        void case_527()
#line 2727 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            codegen.CurrentTypeDef.CurrentEvent.AddAddon(
                    (MethodRef)yyVals[-1 + yyTop]);
        }

        void case_528()
#line 2732 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            codegen.CurrentTypeDef.CurrentEvent.AddRemoveon(
                    (MethodRef)yyVals[-1 + yyTop]);
        }

        void case_529()
#line 2737 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            codegen.CurrentTypeDef.CurrentEvent.AddFire(
                    (MethodRef)yyVals[-1 + yyTop]);
        }

        void case_530()
#line 2742 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            codegen.CurrentTypeDef.CurrentEvent.AddOther(
                    (MethodRef)yyVals[-1 + yyTop]);
        }

        void case_531()
#line 2747 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            if (codegen.CurrentCustomAttrTarget != null)
                codegen.CurrentCustomAttrTarget.AddCustomAttribute((CustomAttr)yyVals[0 + yyTop]);
        }

        void case_535()
#line 2762 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            PropertyDef prop_def = new PropertyDef((FeatureAttr)yyVals[-6 + yyTop], (BaseTypeRef)yyVals[-5 + yyTop],
                    (string)yyVals[-4 + yyTop], (ArrayList)yyVals[-2 + yyTop]);
            codegen.CurrentTypeDef.BeginPropertyDef(prop_def);
            codegen.CurrentCustomAttrTarget = prop_def;

            if (yyVals[0 + yyTop] != null)
            {
                prop_def.AddInitValue((Constant)yyVals[0 + yyTop]);
            }
        }

        void case_545()
#line 2809 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            if (codegen.CurrentCustomAttrTarget != null)
                codegen.CurrentCustomAttrTarget.AddCustomAttribute((CustomAttr)yyVals[0 + yyTop]);
        }

        void case_555()
#line 2842 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            var l = (List<BoolConst>)yyVals[-1 + yyTop];
            yyVal = new ArrayConstant(l?.ToArray())
            {
                ExplicitSize = (int)yyVals[-4 + yyTop]
            };
        }

        void case_558()
#line 2857 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            var c = yyVals[0 + yyTop] as Constant;
            yyVal = c ?? new ArrayConstant(((List<DataConstant>)yyVals[0 + yyTop]).ToArray());
        }

        void case_560()
#line 2866 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            var l = yyVals[-1 + yyTop] as List<DataConstant>;
            if (l == null)
            {
                l = new List<DataConstant>() {
                              (DataConstant) yyVals[-1+yyTop]
                          };
            }

            l.Add((DataConstant)yyVals[0 + yyTop]);
            yyVal = l;
        }

        void case_563()
#line 2889 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            BaseTypeRef owner = (BaseTypeRef)yyVals[-5 + yyTop];
            ArrayList arg_list = (ArrayList)yyVals[-1 + yyTop];
            BaseTypeRef[] param_list;

            if (arg_list != null)
                param_list = (BaseTypeRef[])arg_list.ToArray(typeof(BaseTypeRef));
            else
                param_list = new BaseTypeRef[0];

            yyVal = owner.GetMethodRef((BaseTypeRef)yyVals[-6 + yyTop],
                    (CallConv)yyVals[-7 + yyTop], (string)yyVals[-3 + yyTop], param_list, 0);
        }

        void case_564()
#line 2903 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList arg_list = (ArrayList)yyVals[-1 + yyTop];
            BaseTypeRef[] param_list;

            if (arg_list != null)
                param_list = (BaseTypeRef[])arg_list.ToArray(typeof(BaseTypeRef));
            else
                param_list = new BaseTypeRef[0];

            yyVal = codegen.GetGlobalMethodRef((BaseTypeRef)yyVals[-4 + yyTop], (CallConv)yyVals[-5 + yyTop],
                    (string)yyVals[-3 + yyTop], param_list, 0);
        }

        void case_567()
#line 2926 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            System.Text.UnicodeEncoding ue = new System.Text.UnicodeEncoding();
            PermissionSetAttribute psa = new PermissionSetAttribute((System.Security.Permissions.SecurityAction)(short)yyVals[-2 + yyTop]);
            psa.XML = ue.GetString((byte[])yyVals[0 + yyTop]);
            yyVal = new PermPair((PEAPI.SecurityAction)yyVals[-2 + yyTop], psa.CreatePermissionSet());
        }

        void case_568()
#line 2933 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            PermissionSetAttribute psa = new PermissionSetAttribute((System.Security.Permissions.SecurityAction)(short)yyVals[-1 + yyTop]);
            psa.XML = (string)yyVals[0 + yyTop];
            yyVal = new PermPair((PEAPI.SecurityAction)yyVals[-1 + yyTop], psa.CreatePermissionSet());
        }

        void case_570()
#line 2945 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList list = new ArrayList();
            list.Add(yyVals[0 + yyTop]);
            yyVal = list;
        }

        void case_571()
#line 2951 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList list = (ArrayList)yyVals[-2 + yyTop];
            list.Add(yyVals[0 + yyTop]);
            yyVal = list;
        }

        void case_573()
#line 2965 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList list = new ArrayList();
            list.Add(yyVals[0 + yyTop]);
            yyVal = list;
        }

        void case_574()
#line 2971 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList list = (ArrayList)yyVals[-1 + yyTop];
            list.Add(yyVals[0 + yyTop]);
            yyVal = list;
        }

        void case_575()
#line 2979 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            NameValuePair pair = (NameValuePair)yyVals[0 + yyTop];
            yyVal = new PermissionMember((MemberTypes)yyVals[-2 + yyTop], (BaseTypeRef)yyVals[-1 + yyTop], pair.Name, pair.Value);
        }

        void case_576()
#line 2984 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            NameValuePair pair = (NameValuePair)yyVals[0 + yyTop];
            yyVal = new PermissionMember((MemberTypes)yyVals[-3 + yyTop], (BaseTypeRef)yyVals[-1 + yyTop], pair.Name, pair.Value);
        }

        void case_580()
#line 3007 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList pairs = new ArrayList();
            pairs.Add(yyVals[0 + yyTop]);
            yyVal = pairs;
        }

        void case_581()
#line 3013 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList pairs = (ArrayList)yyVals[-2 + yyTop];
            pairs.Add(yyVals[0 + yyTop]);
            yyVal = pairs;
        }

        void case_610()
#line 3133 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            /* We need to compute the hash ourselves. :-(*/
            /* AssemblyName an = AssemblyName.GetName ((string) $3);*/
        }

        void case_615()
#line 3160 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            codegen.CurrentCustomAttrTarget = null;
            codegen.CurrentDeclSecurityTarget = null;
        }

        void case_616()
#line 3167 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            codegen.SetThisAssembly((string)yyVals[0 + yyTop], (PEAPI.AssemAttr)yyVals[-1 + yyTop]);
            codegen.CurrentCustomAttrTarget = codegen.ThisAssembly;
            codegen.CurrentDeclSecurityTarget = codegen.ThisAssembly;
        }

        void case_634()
#line 3229 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            System.Reflection.AssemblyName asmb_name =
new System.Reflection.AssemblyName();
            asmb_name.Name = (string)yyVals[0 + yyTop];
            codegen.BeginAssemblyRef((string)yyVals[0 + yyTop], asmb_name, (PEAPI.AssemAttr)yyVals[-1 + yyTop]);
        }

        void case_635()
#line 3236 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            System.Reflection.AssemblyName asmb_name =
new System.Reflection.AssemblyName();
            asmb_name.Name = (string)yyVals[-2 + yyTop];
            codegen.BeginAssemblyRef((string)yyVals[0 + yyTop], asmb_name, (PEAPI.AssemAttr)yyVals[-3 + yyTop]);
        }

        void case_644()
#line 3271 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            if (codegen.CurrentCustomAttrTarget != null)
                codegen.CurrentCustomAttrTarget.AddCustomAttribute((CustomAttr)yyVals[0 + yyTop]);
        }

        void case_665()
#line 3316 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            FileStream s = new FileStream((string)yyVals[0 + yyTop], FileMode.Open, FileAccess.Read);
            byte[] buff = new byte[s.Length];
            s.Read(buff, 0, (int)s.Length);
            s.Close();

            codegen.AddManifestResource(new ManifestResource((string)yyVals[0 + yyTop], buff, (yyVals[-1 + yyTop] == null) ? 0 : (uint)yyVals[-1 + yyTop]));
        }

        void case_676()
#line 3345 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            long l = (long)yyVals[0 + yyTop];
            byte[] intb = BitConverter.GetBytes(l);
            yyVal = BitConverter.ToInt32(intb, BitConverter.IsLittleEndian ? 0 : 4);
        }

        void case_679()
#line 3357 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            int i = (int)yyVals[-1 + yyTop];
            byte[] intb = BitConverter.GetBytes(i);
            yyVal = (double)BitConverter.ToSingle(intb, 0);
        }

        void case_680()
#line 3363 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            long l = (long)yyVals[-1 + yyTop];
            byte[] intb = BitConverter.GetBytes(l);
            yyVal = (double)BitConverter.ToSingle(intb, BitConverter.IsLittleEndian ? 0 : 4);
        }

        void case_681()
#line 3369 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            byte[] intb = BitConverter.GetBytes((long)yyVals[-1 + yyTop]);
            yyVal = BitConverter.ToDouble(intb, 0);
        }

        void case_682()
#line 3374 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            byte[] intb = BitConverter.GetBytes((int)yyVals[-1 + yyTop]);
            yyVal = (double)BitConverter.ToSingle(intb, 0);
        }

        void case_685()
#line 3388 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            yyVal = yyVals[-1 + yyTop];
            tokenizer.InByteArray = false;
        }

        void case_687()
#line 3396 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList byte_list = (ArrayList)yyVals[0 + yyTop];
            yyVal = byte_list.ToArray(typeof(byte));
        }

        void case_688()
#line 3403 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList byte_list = new ArrayList();
            byte_list.Add(Convert.ToByte(yyVals[0 + yyTop]));
            yyVal = byte_list;
        }

        void case_689()
#line 3409 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"
        {
            ArrayList byte_list = (ArrayList)yyVals[-1 + yyTop];
            byte_list.Add(Convert.ToByte(yyVals[0 + yyTop]));
        }

#line default
        static readonly short[] yyLhs = {              -1,
    0,    1,    1,    2,    2,    2,    2,    2,    2,    2,
    2,    2,    2,    2,    2,    2,    2,    2,    2,    2,
    2,    2,    2,    2,   19,   19,   19,   19,   20,   20,
   20,    8,   21,   21,   21,   21,   21,    4,   23,    3,
   25,   27,   27,   27,   27,   27,   27,   27,   27,   27,
   27,   27,   27,   27,   27,   27,   27,   27,   27,   27,
   27,   27,   27,   27,   27,   27,   29,   29,   30,   30,
   32,   32,   28,   28,   34,   34,   35,   35,   37,   37,
   38,   38,   31,   31,   31,   31,   31,   31,   31,   33,
   33,   40,   40,   40,   40,   40,   40,   41,   42,   42,
   43,   43,   44,   44,   39,   39,   39,   26,   26,   45,
   45,   45,   45,   45,   45,   45,   45,   45,   45,   45,
   45,   52,   45,   45,   36,   36,   36,   36,   36,   36,
   36,   36,   36,   36,   36,   36,   36,   56,   56,   56,
   56,   56,   56,   56,   56,   56,   56,   56,   56,   56,
   56,   56,   56,   56,   56,   56,   56,   56,   56,   56,
   54,   54,   57,   57,   57,   57,   57,   50,   50,   50,
   58,   58,   58,   58,   58,   58,   58,   59,   59,   59,
   59,   59,   59,   59,   59,   59,   59,   59,   59,   59,
   59,   59,   59,   59,   59,   59,   59,   59,   59,   59,
   59,   59,   59,   59,   59,   59,   59,   59,   59,   59,
   59,   59,   59,   59,   59,   59,   59,   59,   59,   59,
   59,   59,   59,   59,   59,   59,   59,   59,   59,   59,
   61,   61,   61,   61,   61,   61,   61,   61,   61,   61,
   61,   61,   61,   61,   61,   61,   61,   61,   61,   61,
   61,   61,   61,   61,   61,   61,   61,   61,   61,   61,
   61,   61,   61,   61,   61,   61,   61,   61,   61,   61,
   61,   61,   61,   61,   55,   55,    6,   62,   62,   63,
   63,   63,   63,   63,   63,   63,   63,   63,   63,   63,
   63,   63,   63,   63,   64,   64,   65,   65,   68,   68,
   68,   68,   68,   68,   68,   68,   68,   68,   68,   68,
   68,   68,   68,   71,   71,   67,   67,   67,   73,   73,
   74,   75,   75,    7,   76,   76,   78,   78,   78,   77,
   77,   79,   79,   80,   80,   80,   80,   80,   80,   80,
   80,   80,   80,   80,   80,   80,   80,   80,   80,   80,
    5,   81,   81,   83,   83,   83,   83,   83,   83,   83,
   83,   83,   83,   83,   83,   83,   83,   83,   83,   83,
   83,   83,   83,   83,   83,   83,   86,   86,   86,   86,
   86,   86,   86,   86,   86,   86,   86,   86,   86,   86,
   86,   49,   49,   49,   84,   84,   84,   84,   85,   85,
   85,   85,   85,   85,   85,   85,   85,   85,   85,   85,
   85,   85,   85,   53,   53,   87,   87,   88,   88,   88,
   88,   88,   51,   51,   51,   51,   51,   89,   89,   82,
   82,   90,   90,   90,   90,   90,   90,   90,   90,   90,
   90,   90,   90,   90,   90,   90,   90,   90,   90,   90,
   90,   90,   90,   90,   91,   91,   91,   96,   96,   96,
   96,   97,   48,   48,   48,   93,   98,   94,   99,   99,
   99,  100,  100,  101,  101,  101,  101,  103,  103,  103,
  102,  102,  102,   95,   95,   95,   95,   95,   95,   95,
   95,   95,   95,   95,   95,   95,   95,   95,   95,   95,
   95,   95,   95,   95,   95,   95,   92,   92,  105,  105,
  105,  105,  105,  104,  104,  106,  106,  106,   46,  107,
  107,  109,  109,  109,  108,  108,  110,  110,  110,  110,
  110,  110,  110,   47,  111,  113,  113,  113,  113,  112,
  112,  114,  114,  114,  114,  114,  114,   16,   16,   16,
   16,  115,  115,  117,  117,  117,  117,  117,  118,  118,
  119,  119,  116,  116,   15,   15,   15,   15,   15,  122,
  122,  123,  124,  124,  125,  125,  127,  126,  126,  121,
  121,  128,  129,  129,  129,  129,  129,  129,  129,  129,
  120,  120,  120,  120,  120,  120,  120,  120,  120,  120,
  120,  120,  120,  120,  120,   14,   14,   14,    9,    9,
  130,  130,  131,  131,   10,  132,  135,  135,  133,  133,
  136,  136,  136,  136,  136,  136,  136,  137,  137,  137,
  137,  137,   11,  138,  138,  139,  139,  140,  140,  140,
  140,  140,  140,  140,  140,   12,  141,  143,  143,  143,
  143,  143,  143,  143,  143,  143,  143,  142,  142,  144,
  144,  144,  144,   13,  145,  147,  147,  147,  146,  146,
  148,  148,  148,   60,   60,   17,   18,   69,   69,   69,
   69,   69,  149,  151,   72,  150,  150,  152,  152,   70,
   70,   22,   22,   24,   24,   24,   66,   66,  134,  134,
  };
        static readonly short[] yyLen = {           2,
    1,    0,    2,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    2,    2,    3,
    2,    2,    1,    1,    3,    2,    5,    4,    2,    4,
    6,    7,    0,    2,    2,    2,    2,    4,    2,    4,
    6,    0,    2,    2,    3,    3,    3,    3,    3,    3,
    2,    2,    2,    2,    2,    2,    2,    2,    2,    2,
    2,    2,    2,    2,    2,    2,    0,    2,    0,    1,
    2,    3,    0,    3,    0,    3,    1,    3,    0,    3,
    1,    3,    1,    1,    3,    2,    3,    2,    3,    3,
    5,    0,    2,    2,    2,    2,    2,    1,    3,    5,
    1,    3,    1,    3,    4,    5,    1,    0,    2,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    2,
    2,    0,   15,    1,    1,    3,    6,    3,    3,    4,
    2,    2,    2,    5,    5,    7,    1,    1,    1,    1,
    1,    1,    1,    2,    1,    2,    1,    2,    1,    2,
    1,    2,    3,    2,    1,    1,    1,    1,    1,    1,
    1,    3,    0,    1,    1,    3,    2,    2,    2,    1,
    0,    1,    1,    2,    2,    2,    2,    0,    6,    5,
    5,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    2,    1,    2,    1,    2,    1,    2,
    1,    2,    3,    4,    6,    5,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    2,    4,
    1,    2,    2,    1,    2,    1,    2,    1,    2,    1,
    0,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    2,    2,    2,    2,    1,    3,    2,    2,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    2,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    8,    0,    3,    0,
    2,    2,    2,    2,    2,    2,    2,    2,    2,    2,
    2,    5,    2,    2,    0,    2,    0,    2,    4,    4,
    4,    4,    4,    4,    4,    4,    4,    4,    4,    4,
    4,    4,    4,    1,    2,    1,    1,    1,    1,    4,
    1,    1,    2,    2,    4,    2,    0,    1,    1,    3,
    1,    1,    3,    5,    5,    4,    3,    2,    5,    5,
    5,    5,    5,    5,    2,    2,    2,    2,    2,    2,
    4,   11,   14,    0,    2,    2,    2,    2,    2,    2,
    2,    2,    2,    2,    2,    2,    2,    2,    2,    2,
    2,    2,    2,    8,    6,    5,    0,    2,    2,    2,
    2,    2,    2,    2,    2,    2,    2,    4,    4,    4,
    4,    1,    1,    1,    0,    4,    4,    4,    0,    2,
    2,    2,    2,    2,    2,    2,    2,    2,    2,    2,
    2,    2,    2,    0,    1,    1,    3,    2,    3,    1,
    6,    7,    0,    1,    3,    3,    5,    0,    1,    0,
    2,    2,    2,    4,    5,    1,    1,    4,    6,    4,
    4,    3,   15,    1,    5,    1,    2,    1,    1,    1,
    1,    1,    1,    1,    0,    1,    3,    1,    2,    2,
    3,    3,    3,    4,    1,    3,    1,    2,    2,    4,
    4,    1,    2,    3,    2,    2,    2,    2,    2,    2,
    1,    4,    4,    1,    2,    2,    2,    2,    2,    2,
    2,    2,    2,    2,    2,    2,    2,    5,    3,    2,
    2,    4,    3,    6,    2,    4,    7,    9,    0,    1,
    1,    3,    3,    1,    1,    2,    5,    3,    4,    4,
    3,    0,    2,    2,    0,    2,    3,    3,    3,    3,
    1,    1,    1,    4,    8,    0,    2,    2,    2,    0,
    2,    2,    2,    2,    1,    1,    1,    3,    5,    5,
    7,    0,    3,    0,    7,    2,    4,    1,    1,    2,
    1,    4,    8,    6,    6,    3,    4,    3,    6,    1,
    3,    5,    1,    2,    3,    4,    3,    1,    1,    1,
    3,    3,    1,    1,    4,    1,    6,    6,    6,    4,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    2,    3,    8,    4,
    0,    2,    0,    1,    4,    4,    0,    2,    0,    2,
    3,    8,    2,    3,    3,    1,    1,    3,    8,    2,
    3,    1,    4,    5,    7,    0,    2,    8,    3,    3,
    2,    3,    3,    1,    1,    4,    4,    0,    2,    2,
    2,    3,    3,    3,    3,    3,    3,    0,    2,    2,
    3,    1,    3,    4,    3,    0,    2,    2,    0,    2,
    4,    3,    1,    1,    3,    1,    1,    1,    4,    4,
    4,    4,    1,    0,    4,    0,    1,    1,    2,    1,
    1,    1,    1,    1,    3,    1,    0,    1,    0,    2,
  };
        static readonly short[] yyDefRed = {            2,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  354,    0,  666,    0,    0,    0,    0,    0,
    0,    3,    4,    5,    6,    7,    8,    9,   10,   11,
   12,   13,   14,   15,   16,   17,   23,   24,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  617,  648,
    0,  677,   21,  676,   19,    0,    0,  329,  328,    0,
    0,  280,    0,    0,    0,    0,    0,  692,  693,  696,
    0,  694,    0,    0,    0,  591,  592,  593,  594,  595,
  596,  597,  598,  599,  600,  601,  602,  603,  604,  605,
    0,    0,   22,   18,    0,    2,  108,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  324,  331,
  430,  619,  636,  658,  669,  617,  700,    0,    0,   58,
   43,   44,   66,   65,   51,   53,   54,   55,   56,   57,
   59,   60,   61,   62,   63,    0,   64,   52,    0,    0,
    0,    0,    0,  158,  159,  138,  139,  140,  141,  142,
  143,    0,  145,  147,  149,  151,    0,    0,  155,  157,
  156,    0,   84,  160,    0,  125,    0,   83,    0,  137,
    0,    0,  172,  173,    0,    0,  170,    0,    0,    0,
    0,   20,  612,    0,    0,   25,    0,  355,  356,  357,
  358,  370,  371,  369,  359,  360,  361,  362,  365,  363,
  364,  366,  367,  373,    0,  372,  368,  395,    0,    0,
  667,  668,    0,    0,    0,    0,  674,    0,    0,    0,
    0,    0,    0,  332,    0,    0,  350,    0,  349,    0,
  348,    0,  347,    0,  345,    0,  346,    0,    0,  684,
    0,  338,    0,    0,    0,    0,    0,    0,  618,    0,
  650,  649,    0,  651,    0,   46,   45,   47,   48,   49,
   50,   92,    0,    0,    0,    0,   86,   88,    0,    0,
  154,  152,  144,  146,  148,  150,    0,    0,    0,    0,
    0,  553,  132,  131,  133,    0,    0,    0,  168,  169,
  174,  175,  176,  177,    0,    0,  325,  279,    0,  288,
  281,  282,  283,  289,  290,  291,  284,  285,  286,  287,
  293,  294,    0,  614,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  567,    0,   33,   38,   40,   42,
  522,    0,    0,    0,  536,    0,  111,  110,  114,  115,
  116,  118,  117,  124,  119,  109,  112,  113,    0,    0,
  330,    0,    0,    0,    0,    0,    0,  678,    0,    0,
    0,    0,    0,    0,    0,  337,  467,  351,  484,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  436,    0,    0,    0,    0,    0,    0,
    0,  437,  454,  450,  453,  451,  452,    0,  446,  431,
  444,  448,  449,  430,    0,  615,    0,    0,    0,    0,
  627,  626,  620,  633,    0,    0,    0,    0,    0,  645,
  644,  637,  646,    0,    0,    0,  662,  659,  664,    0,
    0,  673,  670,    0,  652,  653,  654,  655,  656,  657,
    0,    0,    0,    0,    0,    0,   87,   89,  126,  153,
    0,    0,   85,    0,  128,  129,  164,    0,    0,  161,
    0,    0,    0,    0,  393,  392,    0,    0,    0,    0,
    0,  550,    0,    0,    0,    0,   27,    0,    0,    0,
    0,    0,    0,    0,    0,  580,    0,    0,  570,  675,
    0,    0,    0,  121,    0,    0,  120,  525,  540,  333,
  336,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  683,  688,    0,    0,  489,  490,  491,  493,  492,
  494,  495,  496,    0,  497,    0,  500,    0,    0,    0,
    0,    0,  514,  505,  515,    0,  485,  486,  487,  488,
  432,    0,    0,    0,  433,    0,    0,    0,    0,    0,
  469,    0,  447,    0,    0,    0,    0,    0,    0,  472,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,   92,   74,
    0,   93,   94,   95,   97,   96,    0,   68,    0,   41,
    0,    0,    0,    0,    0,    0,    0,    0,  130,    0,
  276,    0,  275,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  561,    0,    0,  559,    0,  218,    0,    0,
    0,    0,    0,  183,  184,  185,  186,  187,  188,  189,
  190,  191,  192,  193,    0,  195,  197,  199,  201,  207,
  208,  209,  210,  211,  212,  213,  214,  215,  216,  217,
    0,  221,  224,  226,  230,  228,    0,    0,    0,    0,
   31,    0,    0,  376,  384,  385,  386,  387,  379,  380,
  381,    0,  378,  382,  383,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  565,    0,    0,  569,    0,    0,
   34,   35,   36,   37,  523,  524,    0,    0,    0,    0,
   99,  539,  537,  538,    0,    0,    0,  344,  343,  342,
  341,    0,    0,    0,    0,  339,  340,  335,  334,  685,
  689,    0,    0,    0,    0,  503,    0,    0,  516,    0,
  511,  510,    0,    0,    0,    0,    0,  456,    0,    0,
    0,  442,    0,    0,    0,    0,    0,  466,    0,  480,
  479,  478,    0,  481,  475,  476,  473,  477,  625,  624,
  621,    0,  643,  642,  639,  640,    0,    0,    0,    0,
    0,    0,    0,    0,    0,   98,   90,   71,    0,    0,
    0,    0,   76,    0,  166,  162,  134,  135,    0,  424,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  556,    0,  551,
    0,  560,  229,  225,  223,    0,    0,    0,  227,  194,
  196,  198,  200,  222,  247,  233,  234,  235,  236,  237,
  238,  239,  240,  241,  242,  261,    0,  251,  252,  253,
  254,  255,  256,  257,  258,  259,  232,  262,  263,  264,
  265,  266,  267,  268,  269,  270,  271,  272,  273,  274,
    0,    0,  292,  202,  296,    0,    0,    0,    0,  375,
    0,    0,  396,  397,  398,    0,    0,  690,  691,    0,
    0,    0,  583,  582,  581,    0,  571,   32,    0,    0,
    0,    0,  519,    0,    0,    0,    0,  531,  532,  533,
  526,  534,    0,    0,    0,  545,  546,  547,  541,  679,
  680,  682,  681,    0,    0,    0,  502,    0,    0,    0,
    0,  506,    0,    0,    0,  459,  434,    0,    0,    0,
    0,  441,    0,  471,  470,  440,  474,    0,    0,    0,
    0,  671,    0,   80,    0,   72,  420,    0,    0,    0,
  416,    0,  127,    0,  564,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  243,  244,
  245,  246,  260,    0,    0,  250,  249,  203,    0,    0,
    0,  318,    0,  298,  314,  316,  698,  277,  609,    0,
  388,  389,  390,  391,    0,    0,    0,    0,  579,  578,
    0,  573,    0,    0,  100,    0,    0,    0,    0,    0,
  543,  544,  542,    0,    0,  498,    0,    0,  513,  512,
    0,  462,  457,  461,  435,    0,  445,    0,    0,    0,
    0,   91,    0,  136,    0,    0,    0,  425,    0,  429,
  426,    0,  313,  311,  307,  305,  303,  301,  299,  302,
  300,  312,  308,  306,  304,  562,  310,  309,  557,    0,
    0,    0,  248,    0,    0,  204,    0,  315,  374,    0,
    0,    0,    0,    0,    0,    0,  572,  574,    0,    0,
    0,    0,  527,  529,  530,  528,    0,    0,  504,  517,
  439,    0,  483,  482,    0,    0,    0,  419,  417,  563,
    0,    0,    0,  180,  181,  206,    0,    0,  399,  585,
    0,    0,    0,  590,    0,    0,  575,    0,    0,    0,
    0,    0,    0,    0,    0,  427,  322,    0,    0,  179,
  205,    0,    0,    0,    0,    0,  576,    0,    0,  535,
    0,  507,    0,  622,  638,    0,  555,  323,    0,  413,
  405,  400,  402,  401,  403,  404,  406,  408,  409,  410,
  411,  412,  407,  587,  588,  589,    0,  319,  577,    0,
    0,    0,    0,  399,    0,    0,  508,    0,  422,    0,
    0,    0,    0,  320,    0,    0,    0,    0,    0,    0,
  123,  443,
  };
        protected static readonly short[] yyDgoto = {             1,
    2,   22,   23,   24,   25,   26,   27,   28,   29,   30,
   31,   32,   33,   34,   35,   36,  458,   53,   37,   38,
  491,   72,   39,  165,   40,  222,   51,  263,  444,  590,
  166,  591,  441, 1140,  595,  215,  587,  785,  168,  442,
  787,  399,    0,  169,  346,  347,  348,  924,  925,  524,
  801, 1205,  958,  459,  602,  170,  460,  177,  667,  484,
  871,   62,  181,  669,  877, 1008, 1004,  623,  361,  893,
 1006,  242, 1189, 1148, 1149,   41,  109,   60,  223,  110,
   42,  243,   67,  802, 1153,  479,  960,  961, 1061,  400,
  747,  525,  764,  402,  403,  748,  749,  404,  405,  559,
  560,  765,  561,  534,  743,  535,  349,  716,  492,  911,
  350,  717,  496,  919,   57,  178,  624,  625,  626,   91,
  485,  488,  489, 1021, 1022, 1023, 1137,  486,  894,   64,
  315,   43,  244,   49,  118,  413,    0,   44,  245,  422,
   45,  246,  119,  428,   46,  247,   74,  433,  513,  514,
  365,  515,
  };
        protected static readonly short[] yySindex = {            0,
    0, 6032, -300, -266,  -25,   55,    6, -336, -146, -332,
   72,   55,    0,   84,    0,  330, 1995, 1995,  -25,   55,
   80,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  116,  148,
  441,  155,  180,  190,  218,  244,  -46,  -41,    0,    0,
 1006,    0,    0,    0,    0, 5435,  589,    0,    0,  287,
   55,    0,   55, -132,  260,   14, 1849,    0,    0,    0,
  330,    0,  318,  -78,  318,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 5546, -140,    0,    0,   55,    0,    0,  918,  278,   91,
  358,  366,  413,  442,  463,  335,  360,   27,    0,    0,
    0,    0,    0,    0,    0,    0,    0, -188, -240,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  565,    0,    0, -153,  173,
  283,   95, -233,    0,    0,    0,    0,    0,    0,    0,
    0,  563,    0,    0,    0,    0,  589,  309,    0,    0,
    0,  487,    0,    0,  318,    0,   -7,    0,  362,    0,
  589,  589,    0,    0,  707, 5435,    0,  373,  378,  397,
 4705,    0,    0, -174,  401,    0,   55,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  424,    0,    0,    0,  318,  330,
    0,    0,  318,  216,  187,  448,    0,  223,  484,  454,
 4649, 5710,   -9,    0,  287,   55,    0,   55,    0,   55,
    0,  -25,    0, -144,    0, -144,    0,  479,  511,    0,
  525,    0, 6452,  421,  620,  -54,  -74, -188,    0,  362,
    0,    0,  740,    0,  318,    0,    0,    0,    0,    0,
    0,    0,  319,  330, -124, -173,    0,    0,  309,  340,
    0,    0,    0,    0,    0,    0, 5435,  526,  330,  104,
  -55,    0,    0,    0,    0,  554,  573,  330,    0,    0,
    0,    0,    0,    0, 2893,  -72,    0,    0,  586,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  406,    0,  504,  591,  602,  614, 5652,  318,
  330,   58,  614,  309,    0,  615,    0,    0,    0,    0,
    0, 5546,   55,  386,    0,   55,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  634,  652,
    0,  918,  610,  613,  649,  656,  658,    0,  663,  667,
  668,  673,  614,  614,  686,    0,    0,    0,    0, -173,
  -25, -200, -173,  589, 5435, 5546, -223,  589, 5294,  683,
 -173, -173,   55,    0,  687, -213,   55, 5687, -225,  598,
   55,    0,    0,    0,    0,    0,    0,  690,    0,    0,
    0,    0,    0,    0,  601,    0,  458, -123,  725,   55,
    0,    0,    0,    0,  729,  -94,  731,  743,   55,    0,
    0,    0,    0,  623,  647,  330,    0,    0,    0,  650,
  330,    0,    0, -207,    0,    0,    0,    0,    0,    0,
 -151, -120, -165,  619,   66,  330,    0,    0,    0,    0,
  483, 5435,    0,   68,    0,    0,    0,  748,  159,    0,
 2909, 2909,  318,  655,    0,    0,  318,  768,  776, 1456,
  484,    0, 6229,  681,  781,  803,    0, -212, -243,  -35,
  475,   89,  330,  277,  191,    0,  790,  176,    0,    0,
 -279, 5162,  795,    0,  147, 5134,    0,    0,    0,    0,
    0, -146, -146, -146, -146,  230,  240, -146, -146,   50,
  146,    0,    0,  800,  686,    0,    0,    0,    0,    0,
    0,    0,    0, 5435,    0, 2575,    0,  102,  484, 5435,
  589, 5435,    0,    0,    0, -173,    0,    0,    0,    0,
    0,   55, 5722,  816,    0,  589,  828,   55,  592,  593,
    0,  827,    0, 6539, 5435,  598, -229, -229,  601,    0,
 -229,   55,  525,  484,  525,  835,  525,  525,  484,  525,
  525,  841,  330,  330,  318,  330, -168,  330,    0,    0,
 5435,    0,    0,    0,    0,    0,  287,    0, -165,    0,
  851,  330,  362,  857,  -82,  187,  330,   55,    0, -139,
    0,  858,    0,  859,  348,  848,  516,  865,  872,  873,
  874,  877,  884,  892,  894,  895,  899,  903,  904,  920,
  525,  923,    0,  929, 1485,    0,  718,    0,  733,  726,
  934,  117,  754,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  367,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 6161,    0,    0,    0,    0,    0,  253,  287,  937,  525,
    0,  614, -145,    0,    0,    0,    0,    0,    0,    0,
    0,  945,    0,    0,    0,  946,  952,  955,  958,  964,
  318,  949,  330, -158,    0,  614,  970,    0,  309,  287,
    0,    0,    0,    0,    0,    0,    0,  330,  348,   55,
    0,    0,    0,    0,  344, 1320,  170,    0,    0,    0,
    0,  971,  974,  975,  980,    0,    0,    0,    0,    0,
    0, 2893,    0,  986,  525,    0,  400, 5435,    0, 2575,
    0,    0,  265,  985,  252,  406,  299,    0, 5435, 5722,
 5435,    0,  348,  992,   55,  287,   55,    0,  -62,    0,
    0,    0, -173,    0,    0,    0,    0,    0,    0,    0,
    0,   55,    0,    0,    0,    0,   55,  318,  318,  362,
   55,  362, -120,  187,  405,    0,    0,    0, -165,  362,
  979, 5435,    0,  104,    0,    0,    0,    0,  994,    0,
  528, 5652,   55,  203,  -25,  -25,  -25,  -25, -148, -148,
  -25,  -25,  -25,  -25, 5435,  -25,  -25,    0, 1009,    0,
  999,    0,    0,    0,    0,  614, 1002, 1003,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  422,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 -113,  -60,    0,    0,    0,  375,  997,  951,  484,    0,
  257,  275,    0,    0,    0, 6229, 1005,    0,    0, 1008,
 1012,  484,    0,    0,    0,  320,    0,    0,  318,  767,
 1016,  207,    0,  589,  589,  589,  589,    0,    0,    0,
    0,    0,  589,  589,  589,    0,    0,    0,    0,    0,
    0,    0,    0, 1013,  526,  287,    0,  848, 4860,    0,
 1014,    0, -173,  916, 1020,    0,    0, 5722,  406,  578,
 4860,    0,  937,    0,    0,    0,    0,  778,  779, 1022,
 1025,    0,  287,    0, 5435,    0,    0, 1028, 5652, 1029,
    0,  187,    0,  848,    0, 1017,  406, 1033, 1037, 1040,
 1041, 1042, 1045, 1046, 1047, 1048, 1051, 1054, 1055, 1056,
 1057,  443, 1058, 1061, 1062,   19,   55,   55,    0,    0,
    0,    0,    0, 1035,  614,    0,    0,    0,   55, -107,
  525,    0,  484,    0,    0,    0,    0,    0,    0,   39,
    0,    0,    0,    0,  331,  979,   55, -203,    0,    0,
 -244,    0, 4187,  589,    0,  848,  997,  997,  997,  997,
    0,    0,    0,  348, 1064,    0,  581,  287,    0,    0,
  287,    0,    0,    0,    0, 1063,    0,   55,  287,   55,
   55,    0,  187,    0,  -68,  979,  608,    0, 5652,    0,
    0, 1065,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  614,
 1069, 1078,    0,  484, 1080,    0,   55,    0,    0,  348,
 1083, 1087, 1088, 1089, 1090, 1096,    0,    0,  309, 1110,
 5435,  618,    0,    0,    0,    0,  526,  848,    0,    0,
    0,  348,    0,    0, 1103, 1104, 1112,    0,    0,    0,
  406,  203,  179,    0,    0,    0, 1111, 1113,    0,    0,
   55,   55,   55,    0, 1110, 1107,    0, 2575,  937, 1116,
  635, 1101,   55,   55, 6229,    0,    0, 1117,  203,    0,
    0,  979, 1279, 1124, 1129, 1132,    0, 2038, 1131,    0,
  848,    0, 5757,    0,    0,  364,    0,    0, 1135,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 1137,    0,    0,  348,
  660,  252,  287,    0, 1150,  848,    0, 1143,    0, 1279,
 1156, 1149, 1141,    0, 1160, 1162,  979,  848, 1163,  664,
    0,    0,
  };
        protected static readonly short[] yyRindex = {            0,
    0, 1434, -184, 1590,    0,    0, 5003,  414, 4789, -128,
    0,    0,    0,  963,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, -184,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 5828,    0,    0, 1165,
    0,    0,    0,    0, 3534, 3728, 5828,    0,    0,    0,
    0,    0, 1539,    0, 1169,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0, 4363,
 4363, 4363, 4363, 4363, 4363,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, -118,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 5828,    0,    0,    0,
    0,    0,    0,    0, 1180,    0,    0,    0, 1762,    0,
 5828, 5828,    0,    0,    0,    0,    0, 3192,    0,    0,
    0,    0,    0, 1248,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, 1830,    0,
    0,    0, 1170,    0, 3420, 3922,    0,    0, 4037,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0, 1175,
    0,    0,    0,    0, 1179,    0,    0,    0,    0,    0,
    0,    0, -179,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0, 2635,    0, 2926,
 1181,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0, 2121, 3639, 3817,  167,    0,  889,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0, 1184,    0,    0,    0,    0,    0,
    0,    0,    0, 5828,    0,    0,    0, 5828,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0, 1190,    0,    0,    0,    0,    0,    0,
    0,  534,    0, 1194,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  347,    0,    0,
    0,    0, 1471, 1181,    0,    0,  501,    0,    0, 1196,
 3306,    0,  391,  297,    0,    0,    0,  167,    0,    0,
    0,    0, 4143,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0, 4363, 4363, 4363, 4363,    0,    0, 4363, 4363,    0,
    0,    0,    0,    0, 1193,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, 6626,    0,
 5828,    0,    0,    0,    0,  670,    0,    0,    0,    0,
    0,    0,  689,    0,    0, 5828,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, 6713,    0,
    0,    0,    0,  732,    0,    0,    0,    0, 1000,    0,
    0,    0,    0,    0,  258,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 1199,    0, 2053, 5020,    0,  -40,    0,  403,    0,  494,
    0,    0,    0,    0,    0, 2609,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0, 1201,    0,    0,    0,    0,    0,
    0,    0,  438,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 -163,    0,    0,    0,    0,    0,    0,    0,  761,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 4581, 1206, 4248,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 1100,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0, 6129,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  702,    0,    0,    0,  689,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  469,  531,  377,
    0, 1215,  534,  711,    0,    0,    0,    0,    0, 2344,
 5418,    0,    0, 2926,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  115,    0,    0,    0,    0,    0,  705, 2412,  167,    0,
    0,    0,    0,    0,    0,  391,    0,    0,    0,  717,
    0,  723,    0,    0,    0,    0,    0,    0, 1218,    0,
    0,    0,    0, 5828, 5828, 5828, 5828,    0,    0,    0,
    0,    0, 5828, 5828, 5828,    0,    0,    0,    0,    0,
    0,    0,    0,    0, 1217,    0,    0, 2609,    0, 6234,
    0,    0,    0, 6800,    0,    0,    0,    0,  741,    0,
    0,    0, 6887,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0, 1219,
    0,  -39,    0, 2609,    0, 5863,  429,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0, 4476,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0, 5418,    0,    0,    0,    0,
    0,    0,    0, 5828,    0, 2609, 1971, 1971, 1971, 1971,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  764,    0,  789, 5863,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  459,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 1217, 2609,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  429,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, 1222,    0,
    0, 1217,    0,    0,  391,    0,    0,    0, 1220,    0,
    0, 5418, 1226,    0,    0,    0,    0,    0,    0,    0,
 2609,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  826,    0,    0, 5277,    0,    0,    0, 1229,
    0, 1235,    0,    0,    0,    0, 5418, 2609,    0,    0,
    0,    0,
  };
        protected static readonly short[] yyGindex = {            0,
 1401,    0, 1288,    0, 1289, 1291, -157,    0,    0,    0,
    0,    0,    0,    0, -176, -196,   -6,   -2, -189, -182,
    0,  -47,    0,  -12,    0,    0,    0,  830,    0,    0,
 -421,    0,    0, -270,    0,  -24,  736,    0, -155,  947,
  574, 1306,    0, -117,    0,    0,    0,  -86, -288,  -32,
 -478,    0, -908,    0, 1072, -446,  938,    0, -849,  -83,
    0,  -21,    0,    0, -902,  124,    0, -846, -224, -770,
  379, -175,    0,    0,    0,    0,    0,    0,    0,  -67,
    0, 1139,    0, -197,  352, -459,    0,  496,  433,    0,
  805, -504, -215,    0,    0,  612,    0,    0,    0,    0,
  998, -505,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  931, 1542,
    0,    0,  862,    0,  541,    0,  430,  868,    0,    0,
  693,    0,    0, 1525, 1457,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, 1059,    0,
    0,    0,
  };
        protected static readonly short[] yyTable = {            55,
  250,   73,  278,   75,  216,   66,  469,  453,  219,  455,
  319,  362,  179,   94,  603,  603,   93,   68,  673,   69,
   70,  588,  265, 1097,  176,  342,  739,  401,  674, 1005,
  224,  167,  343,  969,  208,  217, 1015,  367,  139,  344,
 1047,  752,  325,  548,  280,  341,  395,  412,  421,  427,
  432,  184,  766,  396,  180,  768,  182,  543,  209,   54,
  397,  213,   52,  358,  340,  366,  394,  411,  326,   68,
  240,   69,   70,  699,  288,  699,  699,   47,  227,  229,
  231,  233,  235,  237,   68,  393,   69,   67,  220,   54,
  700,  210,   68,  268,   69,   70,  322,  210,   58,   68,
  217,   69,   70,  140,   54,  231,  255, 1091,  231,  231,
  140,   50,  210,  449,   52,  358,  231,  141,  217,  358,
  472,  579,   61,   54,  277,   68,  880,   69,   70,  611,
  434,  611,  611,  262,  267,  217,  580,  218,  289,  290,
  675,  676,  677,  678,  457,  446,  445,  520,   73,  314,
  581,  295,  251,  252,  563,  994,  313,  288,  672,  995,
  582,  454, 1086,  578,  217,  583,  996,  788,  487,  679,
  680,  681,  682, 1087,  551,  701,  702,  353,  253,   68,
  317,   69,   70,  568,   59,  683,  217,  684,  685,   68,
  792,   69,  692,  429,  470,  398,  521,  320,  240,  584,
  281,  781,   54,  482,  367,  793,  281,   54,  468,  998,
  283,  284,  471,  423,  456,   63,  283,  284,  448,  354,
  999,  355,   68,  356,   69,   70,  270,  271,  457,  357,
  703,  704,   77,   78,  478,  430, 1160,   52,  675,  676,
  677,  678,   67, 1169,  272,  493,    7,   77,   78, 1093,
 1094, 1095,  451,  947,  431,  424,  359,  360,  351,  447,
  425,  281,   48,  352,  282,  474,    7,  679,  680,  681,
  682,  283,  284,  186,  426,  463,   56, 1019, 1020,  510,
  511,  495,  467,  683,  500,  684,  685,  187,  528,  527,
  763, 1080,  533,  529,  481, 1166,  295,  240, 1209,  326,
  544,  547,   73,   73,  241,  601,  601,  264,  359,  360,
 1089, 1005,  359,  360,  211,  212,  799,   54,  686, 1117,
  254,  728,  517,  231,  564,  523,  494,  483,  593,  497,
  326,   65,  569,  538,  540,  592,  158,  597,  401,  288,
  762,   68,  550,   69,   70,  530,  322,  288,   95,  288,
  526, 1147,  736,  687,  688,  689,  249,  395,  693,   61,
  699,  226,  265,  516,  396,  593,  522,  956,  518,  519,
  288,  397,  240,  997,  537,  539,  541,  394, 1168,  735,
  545,  585,   96,  549,  552,  288,  219,  770,  163,  771,
  452,  773,  774,  219,  775,  776,  393,  888,  889, 1027,
 1028, 1029, 1030,  566,   68,  708,   69,  183, 1031, 1032,
 1033,  611,  572,  575,   97,  710,  686,  729,  577, 1010,
  900,  111,  675,  676,  677,  678,  326,  596,  599,  586,
   68,  600,   69,   70,  285,  286,  287,  912,  377,  734,
  285,  286,  287,  698,  707,  818,  112,  711,  699, 1037,
 1150,  679,  680,  681,  682,  281,  113,  763,  780,  326,
  782,   71,  695,  696,  942,  283,  284,  683,  691,  684,
  685,  715,  210,   68,  790,   69,   70, 1026,  733,  794,
  718,  719,  720,  721,  114, 1057,  726,  727,  742,  324,
    7,  722,  723,  240,  878,  285,  286,  287,  738,  732,
  913,  724,  725,   11,   12,  737,  398,  740,  761,   68,
  115,   69,   70,  751,   54,  264,   48,  914,  746,  908,
  916,  872,  117,  963,  873,  660,  909,  917,  915,  741,
  759,  874,  185,  910,  918,  744,  932,  933,  891,  786,
   68,  754,   69,  487,   68,   54,   69, 1102,  225,  760,
  377,  377,  377,  377,  694,  769,  784,  326,  321,  927,
  778,  779,  827,  828,  295,  266,   68,  660,   69,   70,
  937,  938,  660,  295,  295,  790, 1100,  140,  660,  377,
  377,  377,  377,  210,  975,  977,  660,   68,  879,   69,
   70,  795,  691,  959,  264,  377,  269,  377,  377,  872,
  686,   68, 1090,   69,   70,   68,  295,   69,   70,  874,
  892,  295,  281,  238,  295,  295,  165,  295,  295,  165,
  875,  295,  283,  284,  295,  295,   61,  265,  228, 1141,
  295,  295,  872,  217,   61, 1193,  230,  295,  239,  295,
  295,  295,  874,  288,  672,  295,  295,  295,  295,  295,
  296,  295,  898,  931, 1035,  297,  295,  295,  295,  178,
  316,  465,  178,   68,  295,   69,  298,  466,  281,  178,
  928,  327,  167,  327,  281,  167,  954,  955,  283,  284,
  327,   61, 1191,  232,  283,  284,  672,  890,  406,  285,
  286,  287,  930,  327,  318,  899,  691,  672,  936,  428,
  428,  428,  902,  901,  697,  672,  182,   98,  945,  182,
   61,  281,  234,  929, 1076,  949,  182, 1202,  323,  467,
   99,  283,  284,  327,  939,  746,  941,  220,  377, 1210,
  220,   61,   68,  236,   69,   70,  663,  220,  935,  443,
  691,    7,  986,  281,   68, 1107,   69,   70,  944,  363,
  946,  281,  407,  283,  284,  279,  948,  408,  888,  889,
  297,  594,  284,  163,  326,  950,  163,  962, 1059,  103,
  951,  394,   17,   18,  952,  103,  409,  967,  663,  103,
  103,  364,  103,  663,  803,  410,  804,  394,  465,  663,
  982,   79, 1003,   79,  466,  240,  968,  663,  661,  965,
  966, 1128,  970,  971,  972,  973,  974,  976,  978,  979,
  980,  981,  452,  983,  984, 1011, 1012,  450,  959,  830,
  831,  832,  833, 1142,  461, 1088,  821,  608,  609,  610,
  611,  612,  613, 1013, 1014,  475,  614,  615,  616,  617,
  661, 1019, 1020,  462,  834,  661,  285,  286,  287, 1045,
  938,  661, 1109,  966, 1046,   68,  473,   69,  959,  661,
   54,  477,  690,  476,  367, 1000,  327,  327,  327,  327,
  327,  327,  217,  490,  989,  990,  991,  992, 1036, 1120,
  966,  501,  619,  620,  502, 1040, 1001,  414,  695, 1139,
  966, 1044,  495,  100,  101,  102,  103,  104,  105,  993,
  498, 1196,  285,  286,  287,  786, 1162,  966,  285,  286,
  287, 1084,   68,  746,   69,   70,  467,   54,  499, 1060,
  503,  327,  327, 1002,  456,  327, 1039,  504,  467,  505,
 1053, 1197,  966,  506, 1055, 1212,  966,  507,  457,  508,
    7,  509,  509, 1135,  509,  285,  286,  287,  106,  107,
  512,  415,  108,  536,  959,  542,  416,  256,  257,  258,
  455,  455,  606,  553,  259,  260,  261,  171,  172,  173,
  174,  175,  697,  458,  458,  417,  418,  285,  286,  287,
 1081, 1082,   81,   81,  419,  285,  286,  287,  584,  584,
 1110, 1101, 1085, 1111,  586,  586, 1123,  321,  562,  623,
  573, 1114,  565,  103,  103,  103,  567, 1118,  570,  959,
 1092, 1096,  460,  460,  697,  273,  274,  275,  276,  697,
  571,  691,  697,  697,  574,  697,  697,  576,  297,  697,
  420,  598,  697,  697, 1121,   82,   82,  297,  697,  697,
  589, 1113,  605, 1115, 1116,  697,  606,  697,  697,  697,
  668, 1159,  623,  697,  697,  697,  697,  697,  670,  697,
  418,  418,  671,  623,  697,  697,  697,  697,  623,  709,
  297,  730,  697, 1060,  265,  297, 1138,  691,  297,  297,
 1127,  297,  297,  623,  623,  297,  750,  623,  297,  297,
  291,  292,  293,  294,  297,  297,  623,  421,  421,  691,
  757,  297,  753,  297,  297,  297,  755,  756,  772,  297,
  297,  297,  297,  297,  777,  297,  555,  556,  557,  558,
  297,  297,  297,  789, 1154, 1155, 1156,  791,  297,  797,
  798,  800,  435,  436,  437,  805, 1164, 1165,  596,  438,
  439,  440,  806,  807,  808, 1199,  695,  809,  695,  695,
 1103, 1104, 1105, 1106,  810,  695,  695,  695,  695,  695,
  695,  695,  811,  695,  812,  813,  695,  695,  695,  814,
  695,  695,  695,  815,  816,  695,  695,  691,  695,  103,
  695,  695,  695,  695,  695, 1198,  695,  695,  695,  695,
  817,  695,  695,  819,  695,  695,  820,   99,  695,  823,
  824,  825,  695,  695,  826,  829,  695,  695,  695,  695,
  695,  695,  695,  695,  876,  695,  695,  695,  881,  882,
  695,  883,  695,  695,  884,  695,  695,  885,  695,  695,
  606,  695,  695,  695,  886,  262,  896,  695,  695,  695,
  695,  695,  920,  695,  695,  921,  922,  613,  695,  695,
  695,  923,  695,  695,  934,  695,  695,  695,  695,  695,
  926,  943,  957,   68,  964,   69,   70,  641,  985,  804,
  987,  988,  606, 1007,  314, 1016,  695,  606, 1017, 1024,
  606,  606, 1018,  606,  606, 1025, 1041, 1034, 1038, 1042,
  606,  606, 1048, 1049,  695, 1050,  606,  606, 1051, 1054,
 1058, 1056, 1062,  606, 1083,  606,  606,  606, 1063,  695,
  695, 1064, 1065, 1066,  606,  606, 1067, 1068, 1069, 1070,
  641,  695, 1071,  606,  606, 1072, 1073, 1074, 1075, 1077,
  606,  641, 1078, 1079, 1108, 1122,  641, 1112, 1124,  695,
  695,  695,  695,  695,  695,  695,  695, 1125,  695, 1126,
  695,  695,  695,  695, 1129,  641,  641,  694, 1130,  694,
  694, 1131, 1132, 1133,  641,  694,  521, 1134,  694, 1136,
  100,  101,  102,  103,  104,  105, 1143, 1144,  694,  694,
 1151,  694, 1145, 1152, 1158,  120, 1161, 1163, 1167,  695,
  695,  695,  695,  695,  695, 1184,  695,  695,  121,  122,
 1185,  695,  123, 1186,  124, 1190, 1194, 1195,  695, 1201,
  641,  125, 1203,  126,  127,  128,  129,  130,  131,  132,
  133,  966,  134,  135,  136,  106,  107, 1204, 1206,  108,
 1207,  326, 1208,    1, 1211,   39,  665,  103,  695,  103,
  103,  616,  695,  695,  326,  647,  103,  103,  103,  103,
  103,  103,  103,  163,  103,  686,  634,  103,  103,  103,
   69,  103,  103,  554,  687,   70,  103,  103,  558,  103,
  104,  103,  103,  103,  103,  103,   73,  103,  103,  103,
  103,  635,  103,  103,  520,  103,  103,   75,  297,  103,
  415,  321,  352,  103,  103,  353,  221,  103,  103,  103,
  103,  103,  103,  103,  103,  122,  103,  103,  103,  337,
  338,  103,  339,  103,  103,  613,  103,  103,  953,  103,
  103,  887,  103,  103,  103,  783, 1052,  345,  103,  103,
  103,  103,  103,  604,  103,  103, 1188,  796,  607,  103,
  103,  103,  554,  103,  103, 1200,  103,  103,  103, 1043,
  103, 1119,  137, 1146,  940,  822,  767,  613,  138,   92,
  897, 1098,  613,  895, 1157,  613,  613,  103,  613,  613,
 1009,  116,  248,  731,    0,  613,  613,    0,    0,  613,
    0,  613,  613,    0,    0,  103,    0,  903,  613,    0,
  613,  613,  613,    0,    0,    0,    0,    0,    0,  613,
  613,  103,  694,  694,  694,    0,    0,    0,  613,  613,
    0,    0,  103,    0,    0,  613,    0,  326,  326,  326,
  326,  326,  326,    0,    0,    0,    0,  904,    0,    0,
  103,  103,  103,  103,  103,  103,  103,  103,    0,  103,
    7,  103,  103,  103,  103,    0,    0,    0,    0,  905,
 1170,    0,    0,   11,   12,    0,    0,    0,    0,    0,
    0, 1171,    0,    0,    0,    0,    0,  906,    0,    0,
    0,    0,  326,  326,    0,    0,  326,  907,    0,    0,
  103,  103,  103,  103,  103,  103,    0,  103,  103,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  103,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0, 1172, 1173, 1174, 1175, 1176, 1177, 1178, 1179, 1180,
 1181, 1182,    0,    0,    0,    0,    0,    0,  104,  103,
  104,  104,    0,  103,  103,    0,    0,  104,  104,  104,
  104,  104,  104,  104,    0,  104,    0,    0,  104,  104,
  104,    0,  104,  104,    0,    0,    0,  104,  104,    0,
  104,  107,  104,  104,  104,  104,  104,    0,  104,  104,
  104,  104,    0,  104,  104,    0,  104,  104,    0,    0,
  104,    0,    0,    0,  104,  104,    0,    0,  104,  104,
  104,  104,  104,  104,  104,  104,    0,  104,  104,  104,
    0,    0,  104,    0,  104,  104,  607,  104,  104,    0,
  104,  104,    0,  104,  104,  104,    0,    0,    0,  104,
  104,  104,  104,  104, 1183,  104,  104,    0,    0,  608,
  104,  104,  104,    0,  104,  104,    0,  104,  104,  104,
    0,  104,    0,    0,    0,    0,    0,   42,  607,   42,
   42,    0,    0,  607,    0,    0,  607,  607,  104,  607,
  607,    0,    0,    0,    0,    0,  607,  607,    0,    0,
    0,    0,  607,  607,    0,    0,  104,    0,    0,  607,
    0,  607,  607,  607,    0,    0,    0,    0,    0,    0,
  607,  607,  104,    0,    0,    0,    0,    0,    0,  607,
  607,    0,    0,  104,    0,    0,  607,  607,  608,  609,
  610,  611,  612,  613,    0,    0,    0,  614,  615,  616,
  617,  104,  104,  104,  104,  104,  104,  104,  104,    0,
  104,    0,  104,  104,  104,  104,  821,  608,  609,  610,
  611,  612,  613,    0,    0,    0,  614,  615,  616,  617,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  618,  619,  620,    0,    0,  621,    0,   42,
    0,  104,  104,  104,  104,  104,  104,    0,  104,  104,
    0,    0,   42,   42,    0,    0,   42,    0,   42,    0,
  104,  618,  619,  620,    0,   42,    0,   42,   42,   42,
   42,   42,   42,   42,   42,    0,   42,   42,   42,    0,
  622,    0,    0,    0,    0,    0,    0,    0,    0,  107,
  104,  107,  107,    0,  104,  104,    0,    0,  107,  107,
  107,    0,  107,  107,  107,    0,  107,    0,    0,  107,
  107,  107,    0,    0,  107,    0,    0,    0,  107,  107,
    0,  107,  105,  107,  107,  107,  107,  107,    0,  107,
  107,  107,  107,    0,  107,  107,    0,  107,  107,    0,
    0,  107,    0,    0,    0,  107,  107,    0,    0,  107,
  107,  107,  107,  107,  107,  107,  107,    0,  107,  107,
  107,    0,    0,  107,    0,  107,  107,  608,  107,  107,
    0,  107,  107,    0,  107,  107,  107,    0,    0,    0,
  107,  107,  107,  107,  107,    0,  107,  107,    0,    0,
  610,  107,  107,  107,    0,  107,  107,    0,  107,  107,
  107,    0,    0,    0,    0,    0,   42,    0,    0,  608,
    0,    0,   42,    0,  608,    0,    0,  608,  608,  107,
  608,  608,    0,    0,    0,    0,    0,  608,  608,    0,
    0,    0,    0,  608,  608,    0,    0,  107,    0,    0,
  608,    0,  608,  608,  608,    0,    0,    0,    0,    0,
    0,  608,  608,  107,    0,    0,    0,    0,    0,    0,
  608,  608,    0,    0,  107,    0,    0,  608,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  107,  107,  107,  107,  107,  107,  107,  107,
    0,  107,    0,  107,  107,  107,  107,  171,  172,  173,
  174,  175,    0,    0,    0,    0,    0,    0,  697,    0,
  188,  189,  190,  191,    0,  192,  193,  194,  195,  196,
  197,  198,    0,    0,    0,    0,    0,    0,  199,    0,
    0,    0,  107,  107,  107,  107,  107,  107,    0,  107,
  107,  200,  201,  202,  203,  204,  205,    0,  697,    0,
    0,  107,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  697,    0,    0,    0,    0,    0,    0,    0,    0,
  697,    0,    0,    0,  697,  697,    0,    0,    0,    0,
  105,  107,  105,  105,    0,  107,  107,    0,  697,  105,
  105,  105,    0,  105,  105,  105,    0,  105,  697,    0,
  105,  105,  105,    0,    0,  105,    0,    0,    0,  105,
  105,    0,  105,  106,  105,  105,  105,  105,  105,    0,
  105,  105,  105,  105,    0,  105,  105,    0,  105,  105,
    0,    0,  105,    0,    0,    0,  105,  105,    0,    0,
  105,  105,  105,  105,  105,  105,  105,  105,    0,  105,
  105,  105,    0,    0,  105,    0,  105,  105,  610,  105,
  105,    0,  105,  105,    0,  105,  105,  105,    0,  206,
  207,  105,  105,  105,  105,  105,    0,  105,  105,    0,
    0,  613,  105,  105,  105,    0,  105,  105,    0,  105,
  105,  105,    0,    0,    0,    0,    0,    0,    0,    0,
  610,    0,    0,    0,    0,  610,    0,    0,  610,  610,
  105,  610,  610,    0,    0,    0,    0,    0,  610,  610,
    0,    0,    0,    0,  610,  610,    0,    0,  105,    0,
    0,  610,    0,  610,  610,  610,    0,    0,    0,    0,
    0,    0,  610,  610,  105,    0,    0,    0,    0,    0,
    0,  610,  610,    0,    0,  105,    0,    0,  610,  821,
  608,  609,  610,  611,  612,  613,    0,    0,    0,  614,
  615,  616,  617,  105,  105,  105,  105,  105,  105,  105,
  105,    0,  105,    0,  105,  105,  105,  105,   76,   77,
   78,   79,   80,   81,   82,   83,   84,   85,   86,   87,
   88,   89,   90,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  619,  620,    0,    0, 1001,
    0,    0,    0,  105,  105,  105,  105,  105,  105,    0,
  105,  105,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  105,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0, 1187,    0,    0,    0,    0,    0,    0,    0,
    0,  106,  105,  106,  106,    0,  105,  105,    0,    0,
  106,  106,  106,    0,  106,  106,  106,    0,  106,    0,
    0,  106,  106,  106,    0,    0,  106,    0,    0,    0,
  106,  106,    0,  106,   75,  106,  106,  106,  106,  106,
    0,  106,  106,  106,  106,    0,  106,  106,    0,  106,
  106,    0,    0,  106,    0,    0,    0,  106,  106,    0,
    0,  106,  106,  106,  106,  106,  106,  106,  106,    0,
  106,  106,  106,    0,    0,  106,    0,  106,  106,  613,
  106,  106,    0,  106,  106,    0,  106,  106,  106,    0,
    0,    0,  106,  106,  106,  106,  106,    0,  106,  106,
    0,    0,    0,  106,  106,  106,    0,  106,  106,    0,
  106,  106,  106,    0,    0,    0,    0,    0,    0,    0,
    0,  613,    0,    0,    0,    0,  613,    0,    0,  613,
  613,  106,  613,  613,    0,    0,    0,    0,    0,  613,
  613,    0,    0,    0,    0,  613,  613,    0,    0,  106,
    0,    0,  613,    0,  613,  613,  613,    0,    0,    0,
    0,    0,    0,  613,  613,  106,    0,    0,    0,    0,
    0,    0,  613,  613,    0,    0,  106,    0,    0,  613,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  106,  106,  106,  106,  106,  106,
  106,  106,    0,  106,    0,  106,  106,  106,  106,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,   68,    0,   69,   70,    0,    0,    0,    0,
    0,    0,    0,  464,  106,  106,  106,  106,  106,  106,
    0,  106,  106,  283,  284,    0,    0,  141,    0,    0,
    0,    0,    0,  106,    0,    0,  395,    0,  395,  395,
    0,    0,    0,    0,    0,    0,    0,  395,    0,    0,
  423,  423,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  395,   75,  106,   75,   75,    0,  106,  106,    0,
    0,   75,   75,   75,    0,   75,   75,   75,    0,   75,
    0,    0,    0,   75,   75,    0,    0,   75,    0,    0,
    0,    0,   75,    0,   75,   75,   75,   75,   75,   75,
   75,    0,   75,   75,   75,   75,    0,   75,   75,    0,
   75,   75,    0,    0,   75,    0,    0,    0,   75,   75,
    0,    0,   75,   75,   75,   75,   75,   75,   75,   75,
    0,   75,   75,   75,    0,    0,   75,    0,   75,   75,
    0,   75,   75,    0,   75,   75,    0,   75,   75,   75,
  142,    0,    0,   75,   75,   75,   75,   75,    0,   75,
   75,    0,    0,    0,   75,   75,   75,    0,   75,   75,
    0,   75,   75,   75,    0,    0,    0,  143,    0,    0,
    0,    0,    0,    0,  395,    0,    0,    0,    0,    0,
    0,    0,   75,    0,    0,  144,  145,  146,  147,  148,
  149,  150,  151,    0,  152,    0,  153,  154,  155,  156,
   75,  395,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,   75,    0,    0,  395,
  395,  395,  395,  395,  395,  395,  395,   75,  395,    0,
  395,  395,  395,  395,    0,  157,  158,  285,  286,  287,
  159,    0,  160,  161,    0,   75,   75,   75,   75,   75,
   75,   75,   75,    0,   75,    0,   75,   75,   75,   75,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  395,
  395,    0,    0,    0,  395,    0,  395,  395,    0,    0,
    0,    0,    0,    0,  162,    0,    0,    0,  163,  164,
    0,    0,    0,    0,    0,   75,   75,   75,   75,   75,
   75,    0,   75,   75,    0,    0,    0,    0,    0,    0,
   68,    0,   69,   70,   75,    0,    0,    0,  395,    0,
    0,  464,  395,  395,    0,    0,   68,    0,   69,   70,
    0,  283,  284,    0,    0,  141,    0,  140,    0,    0,
    0,    0,    0,   75,   75,   75,   75,    0,   75,   75,
    0,  548,   75,   75,   75,    0,   75,   75,   75,    0,
   75,    0,    0,    0,   75,   75,  465,    0,   75,    0,
    0,    0,  466,   75,    0,   75,    0,   75,   75,   75,
   75,   75,    0,   75,   75,   75,   75,    0,   75,   75,
    0,   75,   75,    0,    0,   75,    0,    0,    0,   75,
   75,    0,    0,   75,   75,   75,   75,   75,   75,   75,
   75,    0,   75,   75,   75,    0,    0,   75,    0,   75,
   75,    0,   75,   75,    0,   75,   75,    0,   75,   75,
   75,    0,    0,    0,   75,   75,   75,   75,   75,    0,
   75,   75,    0,    0,    0,   75,   75,   75,    0,   75,
   75,    0,   75,   75,   75,    0,    0,    0,  142,    0,
    0,    0,    0,    0,    0,  549,    0,    0,    0,    0,
    0,    0,    0,   75,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  143,    0,    0,    0,    0,
    0,   75,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  143,    0,  144,  145,  146,  147,  148,  149,  150,
  151,    0,  152,    0,  153,  154,  155,  156,   75,  144,
  145,  146,  147,  148,  149,  150,  151,    0,  152,    0,
  153,  154,  155,  156,    0,    0,   75,   75,   75,   75,
   75,   75,   75,   75,    0,   75,    0,   75,   75,   75,
   75,    0,    0,  157,  158,  285,  286,  287,  159,    0,
  160,  161,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  159,    0,  160,  161,    0,  465,
    0,    0,    0,    0,    0,    0,   75,   75,   75,   75,
   75,   75,    0,   75,   75,    0,    0,    0,    0,    0,
    0,    0,  162,    0,    0,   75,  163,  164,    0,  548,
    0,  548,    0,    0,    0,    0,    0,    0,  548,  548,
    0,    0,    0,  164,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,   75,    0,    0,    0,   75,
   75,  548,    0,  548,  548,  548,  548,  548,    0,  548,
  548,  548,  548,    0,  548,  548,    0,  548,  548,  548,
    0,  548,    0,    0,    0,    0,  548,    0,    0,  548,
  548,    0,  548,  548,  548,  548,  548,    0,  548,  548,
  548,  548,  548,  548,    0,  548,  548,    0,  548,  548,
    0,  548,  548,   29,  548,  548,  548,    0,    0,  548,
  548,  548,  548,  548,  548,    0,  548,  548,  548,  548,
  548,  548,  548,  548,    0,  548,  548,    0,  548,  548,
  548,    0,    0,  549,    0,  549,    0,    0,    0,    0,
    0,    0,  549,  549,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  549,    0,  549,  549,  549,
  549,  549,  548,  549,  549,  549,  549,    0,  549,  549,
    0,  549,  549,  549,    0,  549,    0,    0,    0,    0,
  549,    0,    0,  549,  549,    0,  549,  549,  549,  549,
  549,    0,  549,  549,  549,  549,  549,  549,   30,  549,
  549,    0,  549,  549,    0,  549,  549,    0,  549,  549,
  549,    0,    0,  549,  549,  549,  549,  549,  549,    0,
  549,  549,  549,  549,  549,  549,  549,  549,    0,  549,
  549,    0,  549,  549,  549,    0,    0,  465,    0,  465,
  465,    0,    0,    0,    0,    0,  465,  465,    0,    0,
  465,    0,    0,    0,  465,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  465,
    0,  465,  465,  465,  465,  465,  549,  465,  465,  465,
  465,    0,  465,  465,    0,  465,  465,   26,    0,  465,
    0,    0,    0,    0,  465,    0,    0,  465,  465,    0,
  465,  465,  465,  465,  465,    0,  465,  465,  465,    0,
    0,  465,    0,  465,  465,    0,  465,  465,    0,  465,
  465,    0,  465,  465,  465,    0,    0,    0,  465,  465,
  465,  465,  465,    0,  465,  465,    0,    0,    0,  465,
  465,  465,    0,  465,  465,    0,  465,  465,  465,    0,
    0,   29,    0,   29,    0,    0,    0,    0,    0,    0,
   29,   29,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,   28,    0,    0,    0,
    0,    0,    0,   29,    0,   29,   29,   29,   29,   29,
    0,   29,   29,   29,   29,    0,   29,   29,    0,   29,
   29,   29,    0,   29,    0,    0,    0,    0,   29,    0,
    0,   29,   29,    0,   29,   29,   29,   29,   29,    0,
   29,   29,   29,   29,   29,    0,    0,   29,   29,    0,
    0,   29,    0,   29,   29,    0,   29,   29,   29,    0,
    0,   29,   29,   29,   29,   29,   29,    0,   29,    0,
    0,   29,   29,   29,   29,   29,   30,   29,   30,    0,
   29,   29,   29,    0,    0,   30,   30,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  566,    0,    0,    0,    0,    0,    0,   30,    0,
   30,   30,   30,   30,   30,    0,   30,   30,   30,   30,
    0,   30,   30,    0,   30,   30,   30,    0,   30,    0,
    0,    0,    0,   30,    0,    0,   30,   30,    0,   30,
   30,   30,   30,   30,    0,   30,   30,   30,   30,   30,
    0,    0,   30,   30,    0,    0,   30,    0,   30,   30,
    0,   30,   30,   30,    0,   26,   30,   30,   30,   30,
   30,   30,    0,   30,   26,   26,   30,   30,   30,   30,
   30,    0,   30,    0,    0,   30,   30,   30,    0,    0,
    0,    0,    0,    0,    0,    0,    0,   26,    0,   26,
   26,   26,   26,   26,    0,   26,   26,   26,   26,    0,
   26,   26,    0,   26,   26,   26,  568,   26,    0,    0,
    0,    0,   26,    0,    0,   26,   26,    0,   26,   26,
   26,   26,   26,    0,   26,   26,   26,   26,   26,    0,
    0,   26,   26,    0,    0,   26,    0,   26,   26,    0,
   26,   26,   26,    0,   28,   26,   26,   26,   26,   26,
   26,    0,   26,   28,   28,   26,   26,   26,   26,   26,
    0,   26,    0,    0,   26,   26,   26,    0,    0,    0,
    0,    0,    0,    0,    0,    0,   28,    0,   28,   28,
   28,   28,   28,    0,   28,   28,   28,   28,    0,   28,
   28,    0,   28,   28,   28,    0,   28,    0,    0,    0,
    0,   28,    0,    0,   28,   28,    0,   28,   28,   28,
   28,   28,  463,   28,   28,   28,   28,   28,    0,    0,
   28,   28,    0,    0,   28,    0,   28,   28,    0,   28,
   28,   28,    0,    0,   28,   28,   28,   28,   28,   28,
    0,   28,    0,    0,   28,   28,   28,   28,   28,  566,
   28,  566,    0,   28,   28,   28,    0,    0,  566,  566,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  566,    0,  566,  566,  566,  566,  566,    0,  566,
  566,  566,  566,    0,  566,  566,    0,  566,  566,    0,
    0,  566,    0,    0,    0,    0,  566,    0,    0,  566,
  566,    0,  566,  566,  566,  566,  566,  464,  566,  566,
  566,    0,    0,  566,    0,  566,  566,    0,  566,  566,
    0,  566,  566,    0,  566,  566,  566,    0,    0,    0,
  566,  566,  566,  566,  566,    0,  566,  566,    0,    0,
    0,  566,  566,  566,    0,  566,  566,    0,  566,  566,
  566,    0,    0,    0,  568,    0,  568,    0,    0,    0,
    0,    0,    0,  568,  568,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  568,    0,  568,  568,
  568,  568,  568,    0,  568,  568,  568,  568,    0,  568,
  568,    0,  568,  568,    0,    0,  568,    0,    0,    0,
    0,  568,    0,    0,  568,  568,    0,  568,  568,  568,
  568,  568,  278,  568,  568,  568,    0,    0,  568,    0,
  568,  568,    0,  568,  568,    0,  568,  568,    0,  568,
  568,  568,    0,    0,    0,  568,  568,  568,  568,  568,
    0,  568,  568,    0,    0,    0,  568,  568,  568,    0,
  568,  568,    0,  568,  568,  568,    0,    0,    0,  463,
  463,    0,    0,  463,    0,    0,    0,  463,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  463,    0,  463,  463,  463,  463,  463,    0,
  463,  463,  463,  463,    0,  463,  463,    0,  463,  463,
    0,    0,  463,    0,    0,    0,    0,  463,    0,    0,
  463,  463,    0,  463,  463,  463,  463,  463,    0,  463,
  463,  463,    0,    0,  463,  317,  463,  463,    0,  463,
  463,    0,  463,  463,    0,  463,  463,  463,    0,    0,
    0,  463,  463,  463,  463,  463,    0,  463,  463,    0,
    0,    0,  463,  463,  463,    0,  463,  463,    0,  463,
  463,  463,    0,    0,  464,  464,    0,    0,  464,    0,
    0,    0,  464,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  464,    0,  464,
  464,  464,  464,  464,    0,  464,  464,  464,  464,    0,
  464,  464,    0,  464,  464,    0,    0,  464,    0,    0,
    0,    0,  464,    0,    0,  464,  464,    0,  464,  464,
  464,  464,  464,    0,  464,  464,  464,    0,    0,  464,
    0,  464,  464,    0,  464,  464,    0,  464,  464,    0,
  464,  464,  464,    0,    0,    0,  464,  464,  464,  464,
  464,    0,  464,  464,    0,    0,    0,  464,  464,  464,
    0,  464,  464,    0,  464,  464,  464,    0,    0,  143,
  278,    0,  278,    0,    0,    0,    0,    0,    0,  278,
  278,    0,    0,    0,    0,  278,    0,  144,  145,  146,
  147,  148,  149,  150,  151,    0,  152,    0,  153,  154,
  155,  156,  278,    0,  278,  278,  278,  278,  278,    0,
  278,  278,  278,  278,    0,  278,  278,    0,  278,  278,
    0,    0,  278,    0,    0,    0,    0,  278,    0,    0,
  278,  278,    0,  278,  278,  278,  278,  278,    0,  278,
  278,  278,  159,    0,  160,  161,  278,  278,    0,    0,
  278,    0,  278,  278,    0,  278,  278,  278,    0,    0,
    0,  278,  278,  278,  278,  278,    0,  278,    0,    0,
    0,    0,  278,  278,  278,    0,  278,    0,    0,  278,
  278,  278,    0,  317,    0,  317,    0,    0,    0, 1099,
    0,  164,  317,  317,    0,    0,    0,    0,    0,    0,
    0,    0,  317,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  317,    0,  317,  317,  317,
  317,  317,    0,  317,  317,  317,  317,    0,  317,  317,
    0,  317,  317,    0,    0,  317,    0,    0,    0,    0,
  317,    0,    0,  317,  317,    0,  317,  317,  317,  317,
  317,    0,  317,  317,  317,    0,    0,    0,    0,  317,
  317,    0,    0,  317,    0,  317,  317,    0,  317,  317,
  317,    0,    0,    0,  317,  317,  317,  317,  317,    0,
  317,    0,    0,    0,    0,  317,  317,  317,  394,  317,
  394,  394,  317,  317,  317,    0,    0,  394,  394,  394,
    0,  394,    0,  394,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  394,  394,    0,    0,  394,    0,    0,
  394,    0,  394,  394,  394,  394,  394,    0,  394,  394,
  394,  394,    0,  394,  394,    0,  394,  394,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  394,  394,  394,  394,    0,    0,  394,    0,    0,
    0,    0,    0,    0,  394,  394,  328,    0,  394,    0,
  394,    0,    0,    0,    0,    0,    0,    0,    0,  394,
    0,  394,  394,  394,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  394,    0,    0,  394,    0,  394,
    0,    0,    0,    0,    0,    0,    0,    0,    3,    0,
    0,    0,   68,    4,   69,   70,    5,    6,    0,    7,
    8,    0,    0,  140,    0,    0,    9,   10,    0,    0,
    0,    0,   11,   12,    0,    0,  394,  141,    0,   13,
    0,   14,   15,   16,    0,    0,    0,    0,    0,    0,
   17,   18,    0,    0,    0,    0,    0,    0,    0,   19,
   20,    0,    0,  394,    0,    0,   21,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  394,  394,  394,  394,  394,  394,  394,  394,    0,
  394,    0,  394,  394,  394,  394,  278,    0,  278,  278,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  278,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  394,  394,    0,    0,    0,  394,    0,  394,  394,
    0,    0,  299,  394,    0,    0,  300,  301,  302,  303,
  304,  305,    0,  306,  307,  308,  309,  310,  311,  312,
  142,    0,    0,    0,    0,    0,    0,   68,    0,   69,
   70,    0,    0,    0,    0,    0,    0,    0,  464,    0,
  394,    0,    0,    0,  394,  394,    0,  143,  594,  284,
    0,    0,  141,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  144,  145,  146,  147,  148,
  149,  150,  151,    0,  152,    0,  153,  154,  155,  156,
    0,    0,    0,  465,    0,    0,  278,    0,    0,  466,
  278,  278,  278,  278,  278,  278,    0,  278,  278,  278,
  278,  278,  278,  278,  278,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  157,  158,    0,    0,    0,
  159,    0,  160,  161,    0,    0,    0,    0,    0,    0,
    0,  278,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  278,
  278,  278,  278,  278,  278,  278,  278,    0,  278,    0,
  278,  278,  278,  278,  162,    0,    0,    0,  163,  164,
  552,    0,  552,  552,    0,  142,    0,    0,    0,    0,
    0,  552,    0,    0,    0,    0,    0,  132,    0,  132,
  132,    0,    0,    0,    0,  552,    0,    0,  132,  278,
  278,    0,  143,    0,  278,    0,  278,  278,  132,  132,
    0,    0,  132,    0,    0,    0,    0,    0,    0,    0,
  144,  145,  146,  147,  148,  149,  150,  151,    0,  152,
    0,  153,  154,  155,  156,    0,    0,    0,    0,    0,
    0,    0,    0,  132,    0,    0,    0,    0,  278,  132,
    0,    0,  278,  278,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  157,  158,  285,  286,  287,  159,    0,  160,  161,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  552,  552,  552,  552,  552,    0,    0,    0,    0,
    0,   68,    0,   69,   70,    0,    0,    0,    0,    0,
    0,    0,  140,    0,    0,    0,    0,    0,  552,  162,
    0,    0,    0,  163,  164,    0,  141,    0,    0,   68,
    0,   69,   70,    0,    0,  132,    0,    0,    0,    0,
  214,    0,    0,    0,    0,  552,    0,    0,    0,    0,
    0,    0,    0,    0,  141,    0,    0,    0,    0,    0,
    0,    0,  132,  552,  552,  552,  552,  552,  552,  552,
  552,    0,  552,    0,  552,  552,  552,  552,    0,    0,
  132,  132,  132,  132,  132,  132,  132,  132,    0,  132,
    0,  132,  132,  132,  132,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  552,  552,    0,    0,    0,  552,    0,
  552,  552,  712,    0,    0,    0,    0,    0,    0,    0,
  132,  132,  132,  132,  132,  132,    0,  132,  132,    0,
  713,    0,  714,    0,  395,    0,  395,  395,    0,  142,
    0,    0,    0,    0,    0,  395,    0,  423,    0,  423,
    0,   68,  552,   69,   70,    0,  552,  552,  705,  395,
  706,    0,  214,    0,    0,    0,  143,  142,    0,  132,
    0,    0,    0,  132,  132,    0,  141,    0,    0,    0,
    0,    0,    0,    0,  144,  145,  146,  147,  148,  149,
  150,  151,    0,  152,  143,  153,  154,  155,  156,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  144,  145,  146,  147,  148,  149,  150,  151,
    0,  152,    0,  153,  154,  155,  156,    0,    0,    0,
    0,    0,    0,    0,  157,  158,    0,    0,    0,  159,
    0,  160,  161,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  157,  158,    0,    0,    0,  159,    0,  160,
  161,    0,    0,    0,    0,  395,    0,  395,  395,    0,
    0,    0,  395,  162,    0,    0,  395,  163,  164,  414,
    0,    0,   68,    0,   69,   70,    0,    0,    0,  142,
  395,    0,    0,  140,    0,    0,    0,    0,    0,  395,
    0,  162,    0,    0,    0,  163,  164,  141,    0,    0,
    0,    0,    0,    0,    0,    0,  143,  395,  395,  395,
  395,  395,  395,  395,  395,    0,  395,    0,  395,  395,
  395,  395,    0,    0,  144,  145,  146,  147,  148,  149,
  150,  151,    0,  152,    0,  153,  154,  155,  156,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  395,  395,    0,
    0,    0,  395,    0,  395,  395,    0,    0,    0,    0,
    0,    0,    0,    0,  531,  158,    0,    0,    0,  159,
    0,  160,  161,   68,    0,   69,   70,    0,    0,    0,
    0,    0,    0,    0,  214,  532,    0,    0,    0,    0,
    0,    0,    0,  395,    0,    0,  395,    0,  141,    0,
  395,  395,    0,    0,    0,    0,    0,    0,    0,    0,
  142,    0,    0,  162,    0,    0,    0,  163,  164,    0,
  395,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  143,  395,  395,
  395,  395,  395,  395,  395,  395,    0,  395,    0,  395,
  395,  395,  395,    0,    0,  144,  145,  146,  147,  148,
  149,  150,  151,    0,  152,    0,  153,  154,  155,  156,
    0,    0,    0,    0,    0,    0,    0,    0,    0,   68,
    0,   69,   70,    0,    0,    0,    0,    0,  395,  395,
  480,    0,    0,  395,    0,  395,  395,    0,    0,    0,
    0,    0,    0,    0,  141,  157,  158,    0,    0,    0,
  159,    0,  160,  161,   68,    0,   69,   70,    0,    0,
    0,  142,    0,    0,    0,  214,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  395,    0,  141,
    0,  395,  395,    0,    0,    0,    0,  329,  143,   68,
    0,   69,   70,    0,  162,    0,    0,    0,  163,  164,
  745,    0,    0,    0,    0,    0,  144,  145,  146,  147,
  148,  149,  150,  151,  141,  152,    0,  153,  154,  155,
  156,    0,    0,    0,   68,    0,   69,   70,    0,    0,
    0,    0,    0,    0,  330, 1192,    0,    0,    0,    0,
    7,    8,    0,    0,  331,    0,    0,    9,    0,  141,
    0,    0,    0,   11,   12,    0,  157,  158,    0,    0,
   13,  159,    0,  160,  161,    0,    0,  142,  332,  333,
  334,   17,   18,    0,  335,    0,    0,    0,    0,  336,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  143,  171,    0,  171,  171,    0,
    0,    0,  142,    0,    0,  162,  171,    0,    0,  163,
  164,    0,  144,  145,  146,  147,  148,  149,  150,  151,
  171,  152,    0,  153,  154,  155,  156,    0,    0,  143,
  395,    0,  395,  395,    0,    0,    0,  142,    0,    0,
    0,  395,    0,    0,    0,    0,    0,  144,  145,  146,
  147,  148,  149,  150,  151,  395,  152,    0,  153,  154,
  155,  156,  157,  158,  143,    0,    0,  159,    0,  160,
  161,    0,  142,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  144,  145,  146,  147,  148,  149,  150,  151,
    0,  152,    0,  153,  154,  155,  156,  546,  158,  143,
    0,    0,  159,    0,  160,  161,    0,    0,    0,    0,
    0,  162,    0,    0,    0,  163,  164,  144,  145,  146,
  147,  148,  149,  150,  151,    0,  152,    0,  153,  154,
  155,  156,  157,  158,    0,    0,    0,  159,    0,  160,
  161,    0,    0,  171,    0,    0,  162,    0,    0,    0,
  163,  164,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  157,  158,    0,
  171,    0,  159,    0,  160,  161,    0,    0,  395,    0,
    0,  162,    0,    0,    0,  163,  164,    0,  171,  171,
  171,  171,  171,  171,  171,  171,    0,  171,    0,  171,
  171,  171,  171,    0,    0,  395,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  162,    0,    0,    0,
  163,  164,    0,  395,  395,  395,  395,  395,  395,  395,
  395,    0,  395,    0,  395,  395,  395,  395,  171,  171,
    0,    0,    0,  171,    0,  171,  171,    0,    0,    0,
    0,    3,    0,    0,    0,    0,    4,    0,    0,    5,
    6,    0,    7,    8,    0,    0,    0,    0,    0,    9,
   10,    0,    0,  395,  395,   11,   12,    0,  395,    0,
  395,  395,   13,    0,   14,   15,   16,  171,    0,    0,
    0,  171,  171,   17,   18,    0,  499,    0,  499,    0,
    0,    0,   19,   20,  694,  499,  499,  694,    0,   21,
    0,    0,    0,  694,    0,    0,    0,  694,  694,    0,
  694,    0,  395,    0,    0,    0,  395,  395,  499,    0,
  499,  499,  499,  499,  499,    0,  499,  499,  499,  499,
    0,  499,  499,    0,  499,  499,    0,    0,    0,  835,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  499,
  499,  499,  499,    0,    0,  499,    0,    0,    0,    0,
    0,    0,  499,  499,    0,    0,  499,    0,  499,    0,
    0,    0,    0,    0,    0,    0,    0,  499,    0,  499,
  499,  499,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  518,  499,  518,    0,  499,    0,  499,    0,  694,
  518,  518,  694,    0,    0,    0,    0,    0,  694,    0,
    0,    0,  694,  694,    0,  694,    0,    0,    0,    0,
    0,    0,    0,  518,    0,  518,  518,  518,  518,  518,
    0,  518,  518,  518,  518,    0,  518,  518,    0,  518,
  518,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  518,  518,  518,  518,    0,    0,
  518,    0,    0,    0,    0,    0,    0,  518,  518,    0,
    0,  518,    0,  518,    0,    0,    0,    0,    0,    0,
    0,    0,  518,    0,  518,  518,  518,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  518,    0,  627,
  518,    0,  518,    0,    0,    0,    0,    0,  836,  837,
    0,  838,  839,  840,  841,  842,  843,  844,  845,  846,
  847,    0,    0,    0,    0,    0,  848,  849,  850,  851,
  852,  694,  694,  694,  853,  854,  628,  855,  856,    0,
    0,  629,    0,    0,    0,  857,    0,  630,  858,  859,
  860,  861,  862,  863,  864,  865,  866,  867,  868,  869,
  870,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  631,  632,    0,    0,  633,  634,  635,  636,
  637,  638,  639,  640,  641,  642,  643,  644,  645,    0,
  646,  647,  648,  649,  650,  651,  652,  653,  654,  655,
  656,  657,  658,  659,  660,  661,  662,  663,  664,   68,
    0,   69,  665,    0,    0,    0,    0,    0,  367,  368,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  666,
    0,    0,    0,    0,    0,    0,  694,  694,  694,    0,
    0,  369,    0,  370,  371,  372,  373,  374,    0,  375,
  376,  377,  378,    0,  379,  380,    0,  381,  382,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    7,    8,  383,  384,    0,    0,  385,    0,
    0,    0,    0,    0,    0,   11,   12,    0,    0,  386,
    0,  387,    0,    0,    0,    0,   68,    0,   69,    0,
  388,    0,  389,   17,   18,  367,  758,    0,    0,    0,
    0,    0,    0,    0,    0,  390,    0,    0,  391,    0,
  392,    0,    0,    0,    0,    0,    0,    0,  369,    0,
  370,  371,  372,  373,  374,    0,  375,  376,  377,  378,
    0,  379,  380,    0,  381,  382,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    7,
    8,  383,  384,    0,    0,  385,    0,    0,    0,    0,
    0,    0,   11,   12,    0,    0,  386,    0,  387,    0,
    0,    0,    0,  501,    0,  501,    0,  388,    0,  389,
   17,   18,  501,  501,    0,    0,    0,    0,    0,    0,
    0,    0,  390,    0,    0,  391,    0,  392,    0,    0,
    0,    0,    0,    0,    0,  501,    0,  501,  501,  501,
  501,  501,    0,  501,  501,  501,  501,    0,  501,  501,
    0,  501,  501,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  501,  501,  501,  501,
    0,    0,  501,    0,    0,    0,    0,    0,    0,  501,
  501,    0,    0,  501,    0,  501,    0,    0,    0,    0,
  468,    0,  468,    0,  501,    0,  501,  501,  501,  468,
  468,    0,    0,    0,    0,    0,    0,    0,    0,  501,
    0,    0,  501,    0,  501,    0,    0,    0,    0,    0,
    0,    0,  468,    0,  468,  468,  468,  468,  468,    0,
  468,  468,  468,  468,    0,  468,  468,    0,  468,  468,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  468,  468,  468,  468,    0,    0,  468,
    0,    0,    0,    0,    0,    0,  468,  468,    0,    0,
  468,    0,  468,    0,    0,    0,    0,  438,    0,  438,
    0,  468,    0,  468,  468,  468,  438,  438,    0,    0,
    0,    0,    0,    0,    0,    0,  468,    0,    0,  468,
    0,  468,    0,    0,    0,    0,    0,    0,    0,  438,
    0,  438,  438,  438,  438,  438,    0,  438,  438,  438,
  438,    0,  438,  438,    0,  438,  438,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  438,  438,  438,  438,    0,    0,  438,    0,    0,    0,
    0,    0,    0,  438,  438,    0,    0,  438,    0,  438,
    0,    0,    0,    0,  297,    0,  297,    0,  438,    0,
  438,  438,  438,  297,  297,    0,    0,    0,    0,    0,
    0,    0,    0,  438,    0,    0,  438,    0,  438,    0,
    0,    0,    0,    0,    0,    0,  297,    0,  297,  297,
  297,  297,  297,    0,  297,  297,  297,  297,    0,  297,
  297,    0,  297,  297,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  297,  297,  297,
  297,    0,    0,  297,    0,    0,    0,    0,    0,    0,
  297,  297,    0,    0,  297,    0,  297,    0,    0,    0,
    0,    0,    0,    0,    0,  297,    0,  297,  297,  297,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  297,    0,    0,  297,    0,  297,
  };
        protected static readonly short[] yyCheck = {             6,
  118,   14,  158,   16,   91,   12,  295,  278,   92,  280,
  208,  236,   60,   20,  461,  462,   19,  258,  478,  260,
  261,  443,  140,  268,   57,  222,  531,  243,  272,  876,
   98,   56,  222,  804,   67,  259,  886,  267,   51,  222,
  943,  546,  218,  269,  162,  222,  243,  244,  245,  246,
  247,   64,  558,  243,   61,  561,   63,  271,   71,  263,
  243,   74,  263,  264,  222,  241,  243,  244,  281,  258,
  271,  260,  261,  258,  282,  260,  261,  378,  100,  101,
  102,  103,  104,  105,  258,  243,  260,  267,   95,  263,
  370,  266,  258,  141,  260,  261,  214,  266,  435,  258,
  259,  260,  261,  269,  263,  269,  119, 1016,  272,  273,
  269,  378,  266,  269,  263,  264,  280,  283,  259,  264,
  296,  273,  269,  263,  157,  258,  272,  260,  261,  258,
  248,  260,  261,  287,  141,  259,  288,  278,  171,  172,
  384,  385,  386,  387,  284,  270,  264,  372,  267,  324,
  271,  176,  393,  394,  278,  269,  181,  282,  371,  273,
  281,  279,  270,  371,  259,  286,  280,  589,  324,  413,
  414,  415,  416,  281,  390,  455,  456,  225,  419,  258,
  187,  260,  261,  278,  521,  429,  259,  431,  432,  258,
  273,  260,  481,  268,  267,  243,  372,  210,  271,  320,
  269,  370,  263,  321,  267,  288,  269,  263,  295,  270,
  279,  280,  296,  268,  270,  548,  279,  280,  266,  226,
  281,  228,  258,  230,  260,  261,  460,  461,  284,  232,
  510,  511,  273,  273,  318,  310, 1139,  263,  384,  385,
  386,  387,  422, 1152,  478,  332,  321,  288,  288,  453,
  454,  455,  277,  759,  329,  310,  457,  458,  268,  266,
  315,  269,  563,  273,  272,  313,  321,  413,  414,  415,
  416,  279,  280,  260,  329,  288,  271,  522,  523,  363,
  364,  507,  295,  429,  352,  431,  432,  274,  512,  376,
  520,  273,  379,  377,  319, 1145,    0,  271, 1207,  281,
  514,  388,  421,  422,  278,  461,  462,  343,  457,  458,
  272, 1158,  457,  458,  393,  394,  605,  263,  562,  388,
  561,  272,  370,  487,  408,  373,  333,  270,  446,  336,
  281,  260,  416,  381,  382,  270,  502,  270,  554,  282,
  556,  258,  390,  260,  261,  378,  464,  282,  269,  282,
  375, 1122,  528,  389,  390,  391,  545,  554,  270,  269,
  545,  271,  480,  370,  554,  483,  373,  789,  371,  372,
  282,  554,  271,  487,  381,  382,  383,  554, 1149,  278,
  387,  502,  267,  390,  391,  282,  272,  563,  554,  565,
  287,  567,  568,  279,  570,  571,  554,  556,  557,  904,
  905,  906,  907,  410,  258,  492,  260,  540,  913,  914,
  915,  540,  419,  426,  267,  269,  562,  272,  431,  879,
  709,  267,  384,  385,  386,  387,  281,  452,  270,  550,
  258,  273,  260,  261,  503,  504,  505,  268,  272,  526,
  503,  504,  505,  268,  492,  621,  267,  495,  273,  928,
  272,  413,  414,  415,  416,  269,  267,  520,  576,  281,
  578,  378,  272,  273,  753,  279,  280,  429,  481,  431,
  432,  496,  266,  258,  592,  260,  261,  271,  526,  597,
  502,  503,  504,  505,  267,  964,  508,  509,  536,  267,
  321,  262,  263,  271,  670,  503,  504,  505,  531,  524,
  331,  262,  263,  334,  335,  530,  554,  532,  556,  258,
  267,  260,  261,  546,  263,  343,  563,  348,  543,  716,
  717,  269,  564,  794,  272,  268,  716,  717,  359,  536,
  555,  279,  273,  716,  717,  542,  272,  273,  694,  587,
  258,  548,  260,  699,  258,  263,  260, 1026,  271,  556,
  384,  385,  386,  387,  278,  562,  581,  281,  343,  735,
  573,  574,  446,  447,  268,  283,  258,  310,  260,  261,
  272,  273,  315,  277,  278,  693, 1023,  269,  321,  413,
  414,  415,  416,  266,  809,  810,  329,  258,  672,  260,
  261,  598,  605,  791,  343,  429,  502,  431,  432,  269,
  562,  258,  272,  260,  261,  258,  310,  260,  261,  279,
  694,  315,  269,  279,  318,  319,  270,  321,  322,  273,
  668,  325,  279,  280,  328,  329,  269,  745,  271, 1108,
  334,  335,  269,  259,  269,  272,  271,  341,  279,  343,
  344,  345,  279,  282,  268,  349,  350,  351,  352,  353,
  278,  355,  700,  740,  925,  278,  360,  361,  362,  269,
  260,  314,  272,  258,  368,  260,  270,  320,  269,  279,
  271,  258,  270,  260,  269,  273,  272,  273,  279,  280,
  267,  269, 1161,  271,  279,  280,  310,  694,  268,  503,
  504,  505,  740,  280,  271,  708,  709,  321,  746,  271,
  272,  273,  715,  710,    0,  329,  269,  267,  756,  272,
  269,  269,  271,  738,  272,  763,  279, 1196,  271,  732,
  280,  279,  280,  270,  749,  750,  751,  269,  562, 1208,
  272,  269,  258,  271,  260,  261,  268,  279,  745,  421,
  753,  321,  826,  269,  258, 1034,  260,  261,  755,  271,
  757,  269,  332,  279,  280,  269,  763,  337,  556,  557,
    0,  279,  280,  270,  281,  772,  273,  792,  966,  269,
  777,  271,  352,  353,  781,  275,  356,  802,  310,  279,
  280,  271,  282,  315,  269,  365,  271,  287,  314,  321,
  815,  258,  876,  260,  320,  271,  803,  329,  268,  272,
  273, 1090,  805,  806,  807,  808,  809,  810,  811,  812,
  813,  814,  287,  816,  817,  559,  560,  478, 1016,  453,
  454,  455,  456, 1112,  271, 1001,  452,  453,  454,  455,
  456,  457,  458,  559,  560,  332,  462,  463,  464,  465,
  310,  522,  523,  271,  478,  315,  503,  504,  505,  272,
  273,  321,  272,  273,  941,  258,  271,  260, 1056,  329,
  263,  260,  388,  273,  267,  872,  453,  454,  455,  456,
  457,  458,  259,  259,  453,  454,  455,  456,  926,  272,
  273,  272,  508,  509,  272,  933,  512,  268,    0,  272,
  273,  939,  507,  453,  454,  455,  456,  457,  458,  478,
  267, 1190,  503,  504,  505,  953,  272,  273,  503,  504,
  505,  995,  258,  938,  260,  261,  929,  263,  267,  967,
  272,  508,  509,  549,  270,  512,  933,  272,  941,  272,
  955,  272,  273,  271,  959,  272,  273,  271,  284,  272,
  321,  272,  273, 1099,  272,  503,  504,  505,  508,  509,
  265,  332,  512,  271, 1152,  269,  337,  393,  394,  395,
  272,  273,    0,  274,  400,  401,  402,  379,  380,  381,
  382,  383,  268,  272,  273,  356,  357,  503,  504,  505,
  987,  988,  272,  273,  365,  503,  504,  505,  272,  273,
 1038, 1024,  999, 1041,  272,  273, 1080,  343,  541,  268,
  378, 1049,  278,  503,  504,  505,  278, 1055,  278, 1207,
 1017, 1018,  272,  273,  310,  453,  454,  455,  456,  315,
  278, 1034,  318,  319,  378,  321,  322,  378,  268,  325,
  411,  284,  328,  329, 1059,  272,  273,  277,  334,  335,
  422, 1048,  275, 1050, 1051,  341,  271,  343,  344,  345,
  370, 1138,  321,  349,  350,  351,  352,  353,  278,  355,
  272,  273,  260,  332,  360,  361,  362,  278,  337,  275,
  310,  272,  368, 1121, 1192,  315, 1101, 1090,  318,  319,
 1087,  321,  322,  352,  353,  325,  271,  356,  328,  329,
  384,  385,  386,  387,  334,  335,  365,  272,  273, 1112,
  274,  341,  275,  343,  344,  345,  515,  515,  274,  349,
  350,  351,  352,  353,  274,  355,  516,  517,  518,  519,
  360,  361,  362,  273, 1131, 1132, 1133,  271,  368,  272,
  272,  284,  393,  394,  395,  271, 1143, 1144, 1163,  400,
  401,  402,  271,  271,  271, 1193,  258,  271,  260,  261,
 1027, 1028, 1029, 1030,  271,  267,  268,  269,  270,  271,
  272,  273,  271,  275,  271,  271,  278,  279,  280,  271,
  282,  283,  284,  271,  271,  287,  288, 1190,  290,    0,
  292,  293,  294,  295,  296, 1192,  298,  299,  300,  301,
  271,  303,  304,  271,  306,  307,  268,  280,  310,  482,
  468,  476,  314,  315,  271,  452,  318,  319,  320,  321,
  322,  323,  324,  325,  278,  327,  328,  329,  274,  274,
  332,  270,  334,  335,  270,  337,  338,  270,  340,  341,
  268,  343,  344,  345,  271,  287,  267,  349,  350,  351,
  352,  353,  272,  355,  356,  272,  272,    0,  360,  361,
  362,  272,  364,  365,  270,  367,  368,  369,  370,  371,
  275,  270,  284,  258,  271,  260,  261,  268,  260,  271,
  269,  269,  310,  277,  324,  271,  388,  315,  271,  513,
  318,  319,  271,  321,  322,  270,  371,  275,  275,  270,
  328,  329,  515,  515,  406,  274,  334,  335,  274,  272,
  284,  273,  270,  341,  270,  343,  344,  345,  272,  421,
  422,  272,  272,  272,  352,  353,  272,  272,  272,  272,
  321,  433,  272,  361,  362,  272,  272,  272,  272,  272,
  368,  332,  272,  272,  271,  271,  337,  275,  270,  451,
  452,  453,  454,  455,  456,  457,  458,  270,  460,  270,
  462,  463,  464,  465,  272,  356,  357,  258,  272,  260,
  261,  274,  274,  274,  365,  266,  267,  272,  269,  260,
  453,  454,  455,  456,  457,  458,  274,  274,  279,  280,
  270,  282,  271,  271,  278,  380,  271,  287,  272,  501,
  502,  503,  504,  505,  506,  272,  508,  509,  393,  394,
  272,  513,  397,  272,  399,  275,  272,  271,  520,  260,
  411,  406,  270,  408,  409,  410,  411,  412,  413,  414,
  415,  273,  417,  418,  419,  508,  509,  272,  288,  512,
  271,  267,  271,    0,  272,  267,  267,  258,  550,  260,
  261,  267,  554,  555,  280,  267,  267,  268,  269,  270,
  271,  272,  273,  273,  275,  272,  267,  278,  279,  280,
  267,  282,  283,  268,  272,  267,  287,  288,  268,  290,
    0,  292,  293,  294,  295,  296,  271,  298,  299,  300,
  301,  267,  303,  304,  267,  306,  307,  271,  267,  310,
  272,  272,  267,  314,  315,  267,   96,  318,  319,  320,
  321,  322,  323,  324,  325,  271,  327,  328,  329,  222,
  222,  332,  222,  334,  335,  268,  337,  338,  783,  340,
  341,  692,  343,  344,  345,  579,  953,  222,  349,  350,
  351,  352,  353,  462,  355,  356, 1158,  600,    0,  360,
  361,  362,  404,  364,  365, 1194,  367,  368,  369,  938,
  371, 1056,  547, 1121,  750,  625,  559,  310,  553,   18,
  699, 1021,  315,  696, 1135,  318,  319,  388,  321,  322,
  878,   47,  116,  515,   -1,  328,  329,   -1,   -1,  332,
   -1,  334,  335,   -1,   -1,  406,   -1,  268,  341,   -1,
  343,  344,  345,   -1,   -1,   -1,   -1,   -1,   -1,  352,
  353,  422,  503,  504,  505,   -1,   -1,   -1,  361,  362,
   -1,   -1,  433,   -1,   -1,  368,   -1,  453,  454,  455,
  456,  457,  458,   -1,   -1,   -1,   -1,  308,   -1,   -1,
  451,  452,  453,  454,  455,  456,  457,  458,   -1,  460,
  321,  462,  463,  464,  465,   -1,   -1,   -1,   -1,  330,
  372,   -1,   -1,  334,  335,   -1,   -1,   -1,   -1,   -1,
   -1,  383,   -1,   -1,   -1,   -1,   -1,  348,   -1,   -1,
   -1,   -1,  508,  509,   -1,   -1,  512,  358,   -1,   -1,
  501,  502,  503,  504,  505,  506,   -1,  508,  509,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  520,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  433,  434,  435,  436,  437,  438,  439,  440,  441,
  442,  443,   -1,   -1,   -1,   -1,   -1,   -1,  258,  550,
  260,  261,   -1,  554,  555,   -1,   -1,  267,  268,  269,
  270,  271,  272,  273,   -1,  275,   -1,   -1,  278,  279,
  280,   -1,  282,  283,   -1,   -1,   -1,  287,  288,   -1,
  290,    0,  292,  293,  294,  295,  296,   -1,  298,  299,
  300,  301,   -1,  303,  304,   -1,  306,  307,   -1,   -1,
  310,   -1,   -1,   -1,  314,  315,   -1,   -1,  318,  319,
  320,  321,  322,  323,  324,  325,   -1,  327,  328,  329,
   -1,   -1,  332,   -1,  334,  335,  268,  337,  338,   -1,
  340,  341,   -1,  343,  344,  345,   -1,   -1,   -1,  349,
  350,  351,  352,  353,  546,  355,  356,   -1,   -1,    0,
  360,  361,  362,   -1,  364,  365,   -1,  367,  368,  369,
   -1,  371,   -1,   -1,   -1,   -1,   -1,  258,  310,  260,
  261,   -1,   -1,  315,   -1,   -1,  318,  319,  388,  321,
  322,   -1,   -1,   -1,   -1,   -1,  328,  329,   -1,   -1,
   -1,   -1,  334,  335,   -1,   -1,  406,   -1,   -1,  341,
   -1,  343,  344,  345,   -1,   -1,   -1,   -1,   -1,   -1,
  352,  353,  422,   -1,   -1,   -1,   -1,   -1,   -1,  361,
  362,   -1,   -1,  433,   -1,   -1,  368,  452,  453,  454,
  455,  456,  457,  458,   -1,   -1,   -1,  462,  463,  464,
  465,  451,  452,  453,  454,  455,  456,  457,  458,   -1,
  460,   -1,  462,  463,  464,  465,  452,  453,  454,  455,
  456,  457,  458,   -1,   -1,   -1,  462,  463,  464,  465,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  507,  508,  509,   -1,   -1,  512,   -1,  380,
   -1,  501,  502,  503,  504,  505,  506,   -1,  508,  509,
   -1,   -1,  393,  394,   -1,   -1,  397,   -1,  399,   -1,
  520,  507,  508,  509,   -1,  406,   -1,  408,  409,  410,
  411,  412,  413,  414,  415,   -1,  417,  418,  419,   -1,
  555,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  258,
  550,  260,  261,   -1,  554,  555,   -1,   -1,  267,  268,
  269,   -1,  271,  272,  273,   -1,  275,   -1,   -1,  278,
  279,  280,   -1,   -1,  283,   -1,   -1,   -1,  287,  288,
   -1,  290,    0,  292,  293,  294,  295,  296,   -1,  298,
  299,  300,  301,   -1,  303,  304,   -1,  306,  307,   -1,
   -1,  310,   -1,   -1,   -1,  314,  315,   -1,   -1,  318,
  319,  320,  321,  322,  323,  324,  325,   -1,  327,  328,
  329,   -1,   -1,  332,   -1,  334,  335,  268,  337,  338,
   -1,  340,  341,   -1,  343,  344,  345,   -1,   -1,   -1,
  349,  350,  351,  352,  353,   -1,  355,  356,   -1,   -1,
    0,  360,  361,  362,   -1,  364,  365,   -1,  367,  368,
  369,   -1,   -1,   -1,   -1,   -1,  547,   -1,   -1,  310,
   -1,   -1,  553,   -1,  315,   -1,   -1,  318,  319,  388,
  321,  322,   -1,   -1,   -1,   -1,   -1,  328,  329,   -1,
   -1,   -1,   -1,  334,  335,   -1,   -1,  406,   -1,   -1,
  341,   -1,  343,  344,  345,   -1,   -1,   -1,   -1,   -1,
   -1,  352,  353,  422,   -1,   -1,   -1,   -1,   -1,   -1,
  361,  362,   -1,   -1,  433,   -1,   -1,  368,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  451,  452,  453,  454,  455,  456,  457,  458,
   -1,  460,   -1,  462,  463,  464,  465,  379,  380,  381,
  382,  383,   -1,   -1,   -1,   -1,   -1,   -1,  268,   -1,
  392,  393,  394,  395,   -1,  397,  398,  399,  400,  401,
  402,  403,   -1,   -1,   -1,   -1,   -1,   -1,  410,   -1,
   -1,   -1,  501,  502,  503,  504,  505,  506,   -1,  508,
  509,  423,  424,  425,  426,  427,  428,   -1,  308,   -1,
   -1,  520,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  321,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  330,   -1,   -1,   -1,  334,  335,   -1,   -1,   -1,   -1,
  258,  550,  260,  261,   -1,  554,  555,   -1,  348,  267,
  268,  269,   -1,  271,  272,  273,   -1,  275,  358,   -1,
  278,  279,  280,   -1,   -1,  283,   -1,   -1,   -1,  287,
  288,   -1,  290,    0,  292,  293,  294,  295,  296,   -1,
  298,  299,  300,  301,   -1,  303,  304,   -1,  306,  307,
   -1,   -1,  310,   -1,   -1,   -1,  314,  315,   -1,   -1,
  318,  319,  320,  321,  322,  323,  324,  325,   -1,  327,
  328,  329,   -1,   -1,  332,   -1,  334,  335,  268,  337,
  338,   -1,  340,  341,   -1,  343,  344,  345,   -1,  551,
  552,  349,  350,  351,  352,  353,   -1,  355,  356,   -1,
   -1,    0,  360,  361,  362,   -1,  364,  365,   -1,  367,
  368,  369,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  310,   -1,   -1,   -1,   -1,  315,   -1,   -1,  318,  319,
  388,  321,  322,   -1,   -1,   -1,   -1,   -1,  328,  329,
   -1,   -1,   -1,   -1,  334,  335,   -1,   -1,  406,   -1,
   -1,  341,   -1,  343,  344,  345,   -1,   -1,   -1,   -1,
   -1,   -1,  352,  353,  422,   -1,   -1,   -1,   -1,   -1,
   -1,  361,  362,   -1,   -1,  433,   -1,   -1,  368,  452,
  453,  454,  455,  456,  457,  458,   -1,   -1,   -1,  462,
  463,  464,  465,  451,  452,  453,  454,  455,  456,  457,
  458,   -1,  460,   -1,  462,  463,  464,  465,  524,  525,
  526,  527,  528,  529,  530,  531,  532,  533,  534,  535,
  536,  537,  538,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  508,  509,   -1,   -1,  512,
   -1,   -1,   -1,  501,  502,  503,  504,  505,  506,   -1,
  508,  509,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  520,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  555,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  258,  550,  260,  261,   -1,  554,  555,   -1,   -1,
  267,  268,  269,   -1,  271,  272,  273,   -1,  275,   -1,
   -1,  278,  279,  280,   -1,   -1,  283,   -1,   -1,   -1,
  287,  288,   -1,  290,    0,  292,  293,  294,  295,  296,
   -1,  298,  299,  300,  301,   -1,  303,  304,   -1,  306,
  307,   -1,   -1,  310,   -1,   -1,   -1,  314,  315,   -1,
   -1,  318,  319,  320,  321,  322,  323,  324,  325,   -1,
  327,  328,  329,   -1,   -1,  332,   -1,  334,  335,  268,
  337,  338,   -1,  340,  341,   -1,  343,  344,  345,   -1,
   -1,   -1,  349,  350,  351,  352,  353,   -1,  355,  356,
   -1,   -1,   -1,  360,  361,  362,   -1,  364,  365,   -1,
  367,  368,  369,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  310,   -1,   -1,   -1,   -1,  315,   -1,   -1,  318,
  319,  388,  321,  322,   -1,   -1,   -1,   -1,   -1,  328,
  329,   -1,   -1,   -1,   -1,  334,  335,   -1,   -1,  406,
   -1,   -1,  341,   -1,  343,  344,  345,   -1,   -1,   -1,
   -1,   -1,   -1,  352,  353,  422,   -1,   -1,   -1,   -1,
   -1,   -1,  361,  362,   -1,   -1,  433,   -1,   -1,  368,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  451,  452,  453,  454,  455,  456,
  457,  458,   -1,  460,   -1,  462,  463,  464,  465,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  258,   -1,  260,  261,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  269,  501,  502,  503,  504,  505,  506,
   -1,  508,  509,  279,  280,   -1,   -1,  283,   -1,   -1,
   -1,   -1,   -1,  520,   -1,   -1,  258,   -1,  260,  261,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  269,   -1,   -1,
  272,  273,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  283,  258,  550,  260,  261,   -1,  554,  555,   -1,
   -1,  267,  268,  269,   -1,  271,  272,  273,   -1,  275,
   -1,   -1,   -1,  279,  280,   -1,   -1,  283,   -1,   -1,
   -1,   -1,  288,   -1,  290,    0,  292,  293,  294,  295,
  296,   -1,  298,  299,  300,  301,   -1,  303,  304,   -1,
  306,  307,   -1,   -1,  310,   -1,   -1,   -1,  314,  315,
   -1,   -1,  318,  319,  320,  321,  322,  323,  324,  325,
   -1,  327,  328,  329,   -1,   -1,  332,   -1,  334,  335,
   -1,  337,  338,   -1,  340,  341,   -1,  343,  344,  345,
  406,   -1,   -1,  349,  350,  351,  352,  353,   -1,  355,
  356,   -1,   -1,   -1,  360,  361,  362,   -1,  364,  365,
   -1,  367,  368,  369,   -1,   -1,   -1,  433,   -1,   -1,
   -1,   -1,   -1,   -1,  406,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  388,   -1,   -1,  451,  452,  453,  454,  455,
  456,  457,  458,   -1,  460,   -1,  462,  463,  464,  465,
  406,  433,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  422,   -1,   -1,  451,
  452,  453,  454,  455,  456,  457,  458,  433,  460,   -1,
  462,  463,  464,  465,   -1,  501,  502,  503,  504,  505,
  506,   -1,  508,  509,   -1,  451,  452,  453,  454,  455,
  456,  457,  458,   -1,  460,   -1,  462,  463,  464,  465,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  501,
  502,   -1,   -1,   -1,  506,   -1,  508,  509,   -1,   -1,
   -1,   -1,   -1,   -1,  550,   -1,   -1,   -1,  554,  555,
   -1,   -1,   -1,   -1,   -1,  501,  502,  503,  504,  505,
  506,   -1,  508,  509,   -1,   -1,   -1,   -1,   -1,   -1,
  258,   -1,  260,  261,  520,   -1,   -1,   -1,  550,   -1,
   -1,  269,  554,  555,   -1,   -1,  258,   -1,  260,  261,
   -1,  279,  280,   -1,   -1,  283,   -1,  269,   -1,   -1,
   -1,   -1,   -1,  258,  550,  260,  261,   -1,  554,  555,
   -1,    0,  267,  268,  269,   -1,  271,  272,  273,   -1,
  275,   -1,   -1,   -1,  279,  280,  314,   -1,  283,   -1,
   -1,   -1,  320,  288,   -1,  290,   -1,  292,  293,  294,
  295,  296,   -1,  298,  299,  300,  301,   -1,  303,  304,
   -1,  306,  307,   -1,   -1,  310,   -1,   -1,   -1,  314,
  315,   -1,   -1,  318,  319,  320,  321,  322,  323,  324,
  325,   -1,  327,  328,  329,   -1,   -1,  332,   -1,  334,
  335,   -1,  337,  338,   -1,  340,  341,   -1,  343,  344,
  345,   -1,   -1,   -1,  349,  350,  351,  352,  353,   -1,
  355,  356,   -1,   -1,   -1,  360,  361,  362,   -1,  364,
  365,   -1,  367,  368,  369,   -1,   -1,   -1,  406,   -1,
   -1,   -1,   -1,   -1,   -1,    0,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  388,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  433,   -1,   -1,   -1,   -1,
   -1,  406,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  433,   -1,  451,  452,  453,  454,  455,  456,  457,
  458,   -1,  460,   -1,  462,  463,  464,  465,  433,  451,
  452,  453,  454,  455,  456,  457,  458,   -1,  460,   -1,
  462,  463,  464,  465,   -1,   -1,  451,  452,  453,  454,
  455,  456,  457,  458,   -1,  460,   -1,  462,  463,  464,
  465,   -1,   -1,  501,  502,  503,  504,  505,  506,   -1,
  508,  509,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  506,   -1,  508,  509,   -1,    0,
   -1,   -1,   -1,   -1,   -1,   -1,  501,  502,  503,  504,
  505,  506,   -1,  508,  509,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  550,   -1,   -1,  520,  554,  555,   -1,  258,
   -1,  260,   -1,   -1,   -1,   -1,   -1,   -1,  267,  268,
   -1,   -1,   -1,  555,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  550,   -1,   -1,   -1,  554,
  555,  290,   -1,  292,  293,  294,  295,  296,   -1,  298,
  299,  300,  301,   -1,  303,  304,   -1,  306,  307,  308,
   -1,  310,   -1,   -1,   -1,   -1,  315,   -1,   -1,  318,
  319,   -1,  321,  322,  323,  324,  325,   -1,  327,  328,
  329,  330,  331,  332,   -1,  334,  335,   -1,  337,  338,
   -1,  340,  341,    0,  343,  344,  345,   -1,   -1,  348,
  349,  350,  351,  352,  353,   -1,  355,  356,  357,  358,
  359,  360,  361,  362,   -1,  364,  365,   -1,  367,  368,
  369,   -1,   -1,  258,   -1,  260,   -1,   -1,   -1,   -1,
   -1,   -1,  267,  268,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  290,   -1,  292,  293,  294,
  295,  296,  411,  298,  299,  300,  301,   -1,  303,  304,
   -1,  306,  307,  308,   -1,  310,   -1,   -1,   -1,   -1,
  315,   -1,   -1,  318,  319,   -1,  321,  322,  323,  324,
  325,   -1,  327,  328,  329,  330,  331,  332,    0,  334,
  335,   -1,  337,  338,   -1,  340,  341,   -1,  343,  344,
  345,   -1,   -1,  348,  349,  350,  351,  352,  353,   -1,
  355,  356,  357,  358,  359,  360,  361,  362,   -1,  364,
  365,   -1,  367,  368,  369,   -1,   -1,  258,   -1,  260,
  261,   -1,   -1,   -1,   -1,   -1,  267,  268,   -1,   -1,
  271,   -1,   -1,   -1,  275,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  290,
   -1,  292,  293,  294,  295,  296,  411,  298,  299,  300,
  301,   -1,  303,  304,   -1,  306,  307,    0,   -1,  310,
   -1,   -1,   -1,   -1,  315,   -1,   -1,  318,  319,   -1,
  321,  322,  323,  324,  325,   -1,  327,  328,  329,   -1,
   -1,  332,   -1,  334,  335,   -1,  337,  338,   -1,  340,
  341,   -1,  343,  344,  345,   -1,   -1,   -1,  349,  350,
  351,  352,  353,   -1,  355,  356,   -1,   -1,   -1,  360,
  361,  362,   -1,  364,  365,   -1,  367,  368,  369,   -1,
   -1,  258,   -1,  260,   -1,   -1,   -1,   -1,   -1,   -1,
  267,  268,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,    0,   -1,   -1,   -1,
   -1,   -1,   -1,  290,   -1,  292,  293,  294,  295,  296,
   -1,  298,  299,  300,  301,   -1,  303,  304,   -1,  306,
  307,  308,   -1,  310,   -1,   -1,   -1,   -1,  315,   -1,
   -1,  318,  319,   -1,  321,  322,  323,  324,  325,   -1,
  327,  328,  329,  330,  331,   -1,   -1,  334,  335,   -1,
   -1,  338,   -1,  340,  341,   -1,  343,  344,  345,   -1,
   -1,  348,  349,  350,  351,  352,  353,   -1,  355,   -1,
   -1,  358,  359,  360,  361,  362,  258,  364,  260,   -1,
  367,  368,  369,   -1,   -1,  267,  268,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,    0,   -1,   -1,   -1,   -1,   -1,   -1,  290,   -1,
  292,  293,  294,  295,  296,   -1,  298,  299,  300,  301,
   -1,  303,  304,   -1,  306,  307,  308,   -1,  310,   -1,
   -1,   -1,   -1,  315,   -1,   -1,  318,  319,   -1,  321,
  322,  323,  324,  325,   -1,  327,  328,  329,  330,  331,
   -1,   -1,  334,  335,   -1,   -1,  338,   -1,  340,  341,
   -1,  343,  344,  345,   -1,  258,  348,  349,  350,  351,
  352,  353,   -1,  355,  267,  268,  358,  359,  360,  361,
  362,   -1,  364,   -1,   -1,  367,  368,  369,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  290,   -1,  292,
  293,  294,  295,  296,   -1,  298,  299,  300,  301,   -1,
  303,  304,   -1,  306,  307,  308,    0,  310,   -1,   -1,
   -1,   -1,  315,   -1,   -1,  318,  319,   -1,  321,  322,
  323,  324,  325,   -1,  327,  328,  329,  330,  331,   -1,
   -1,  334,  335,   -1,   -1,  338,   -1,  340,  341,   -1,
  343,  344,  345,   -1,  258,  348,  349,  350,  351,  352,
  353,   -1,  355,  267,  268,  358,  359,  360,  361,  362,
   -1,  364,   -1,   -1,  367,  368,  369,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  290,   -1,  292,  293,
  294,  295,  296,   -1,  298,  299,  300,  301,   -1,  303,
  304,   -1,  306,  307,  308,   -1,  310,   -1,   -1,   -1,
   -1,  315,   -1,   -1,  318,  319,   -1,  321,  322,  323,
  324,  325,    0,  327,  328,  329,  330,  331,   -1,   -1,
  334,  335,   -1,   -1,  338,   -1,  340,  341,   -1,  343,
  344,  345,   -1,   -1,  348,  349,  350,  351,  352,  353,
   -1,  355,   -1,   -1,  358,  359,  360,  361,  362,  258,
  364,  260,   -1,  367,  368,  369,   -1,   -1,  267,  268,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  290,   -1,  292,  293,  294,  295,  296,   -1,  298,
  299,  300,  301,   -1,  303,  304,   -1,  306,  307,   -1,
   -1,  310,   -1,   -1,   -1,   -1,  315,   -1,   -1,  318,
  319,   -1,  321,  322,  323,  324,  325,    0,  327,  328,
  329,   -1,   -1,  332,   -1,  334,  335,   -1,  337,  338,
   -1,  340,  341,   -1,  343,  344,  345,   -1,   -1,   -1,
  349,  350,  351,  352,  353,   -1,  355,  356,   -1,   -1,
   -1,  360,  361,  362,   -1,  364,  365,   -1,  367,  368,
  369,   -1,   -1,   -1,  258,   -1,  260,   -1,   -1,   -1,
   -1,   -1,   -1,  267,  268,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  290,   -1,  292,  293,
  294,  295,  296,   -1,  298,  299,  300,  301,   -1,  303,
  304,   -1,  306,  307,   -1,   -1,  310,   -1,   -1,   -1,
   -1,  315,   -1,   -1,  318,  319,   -1,  321,  322,  323,
  324,  325,    0,  327,  328,  329,   -1,   -1,  332,   -1,
  334,  335,   -1,  337,  338,   -1,  340,  341,   -1,  343,
  344,  345,   -1,   -1,   -1,  349,  350,  351,  352,  353,
   -1,  355,  356,   -1,   -1,   -1,  360,  361,  362,   -1,
  364,  365,   -1,  367,  368,  369,   -1,   -1,   -1,  267,
  268,   -1,   -1,  271,   -1,   -1,   -1,  275,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  290,   -1,  292,  293,  294,  295,  296,   -1,
  298,  299,  300,  301,   -1,  303,  304,   -1,  306,  307,
   -1,   -1,  310,   -1,   -1,   -1,   -1,  315,   -1,   -1,
  318,  319,   -1,  321,  322,  323,  324,  325,   -1,  327,
  328,  329,   -1,   -1,  332,    0,  334,  335,   -1,  337,
  338,   -1,  340,  341,   -1,  343,  344,  345,   -1,   -1,
   -1,  349,  350,  351,  352,  353,   -1,  355,  356,   -1,
   -1,   -1,  360,  361,  362,   -1,  364,  365,   -1,  367,
  368,  369,   -1,   -1,  267,  268,   -1,   -1,  271,   -1,
   -1,   -1,  275,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  290,   -1,  292,
  293,  294,  295,  296,   -1,  298,  299,  300,  301,   -1,
  303,  304,   -1,  306,  307,   -1,   -1,  310,   -1,   -1,
   -1,   -1,  315,   -1,   -1,  318,  319,   -1,  321,  322,
  323,  324,  325,   -1,  327,  328,  329,   -1,   -1,  332,
   -1,  334,  335,   -1,  337,  338,   -1,  340,  341,   -1,
  343,  344,  345,   -1,   -1,   -1,  349,  350,  351,  352,
  353,   -1,  355,  356,   -1,   -1,   -1,  360,  361,  362,
   -1,  364,  365,   -1,  367,  368,  369,   -1,   -1,  433,
  258,   -1,  260,   -1,   -1,   -1,   -1,   -1,   -1,  267,
  268,   -1,   -1,   -1,   -1,  273,   -1,  451,  452,  453,
  454,  455,  456,  457,  458,   -1,  460,   -1,  462,  463,
  464,  465,  290,   -1,  292,  293,  294,  295,  296,   -1,
  298,  299,  300,  301,   -1,  303,  304,   -1,  306,  307,
   -1,   -1,  310,   -1,   -1,   -1,   -1,  315,   -1,   -1,
  318,  319,   -1,  321,  322,  323,  324,  325,   -1,  327,
  328,  329,  506,   -1,  508,  509,  334,  335,   -1,   -1,
  338,   -1,  340,  341,   -1,  343,  344,  345,   -1,   -1,
   -1,  349,  350,  351,  352,  353,   -1,  355,   -1,   -1,
   -1,   -1,  360,  361,  362,   -1,  364,   -1,   -1,  367,
  368,  369,   -1,  258,   -1,  260,   -1,   -1,   -1,  553,
   -1,  555,  267,  268,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  277,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  290,   -1,  292,  293,  294,
  295,  296,   -1,  298,  299,  300,  301,   -1,  303,  304,
   -1,  306,  307,   -1,   -1,  310,   -1,   -1,   -1,   -1,
  315,   -1,   -1,  318,  319,   -1,  321,  322,  323,  324,
  325,   -1,  327,  328,  329,   -1,   -1,   -1,   -1,  334,
  335,   -1,   -1,  338,   -1,  340,  341,   -1,  343,  344,
  345,   -1,   -1,   -1,  349,  350,  351,  352,  353,   -1,
  355,   -1,   -1,   -1,   -1,  360,  361,  362,  258,  364,
  260,  261,  367,  368,  369,   -1,   -1,  267,  268,  269,
   -1,  271,   -1,  273,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  283,  284,   -1,   -1,  287,   -1,   -1,
  290,   -1,  292,  293,  294,  295,  296,   -1,  298,  299,
  300,  301,   -1,  303,  304,   -1,  306,  307,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  321,  322,  323,  324,   -1,   -1,  327,   -1,   -1,
   -1,   -1,   -1,   -1,  334,  335,  268,   -1,  338,   -1,
  340,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  349,
   -1,  351,  352,  353,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  364,   -1,   -1,  367,   -1,  369,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  310,   -1,
   -1,   -1,  258,  315,  260,  261,  318,  319,   -1,  321,
  322,   -1,   -1,  269,   -1,   -1,  328,  329,   -1,   -1,
   -1,   -1,  334,  335,   -1,   -1,  406,  283,   -1,  341,
   -1,  343,  344,  345,   -1,   -1,   -1,   -1,   -1,   -1,
  352,  353,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  361,
  362,   -1,   -1,  433,   -1,   -1,  368,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  451,  452,  453,  454,  455,  456,  457,  458,   -1,
  460,   -1,  462,  463,  464,  465,  258,   -1,  260,  261,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  283,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  501,  502,   -1,   -1,   -1,  506,   -1,  508,  509,
   -1,   -1,  388,  513,   -1,   -1,  392,  393,  394,  395,
  396,  397,   -1,  399,  400,  401,  402,  403,  404,  405,
  406,   -1,   -1,   -1,   -1,   -1,   -1,  258,   -1,  260,
  261,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  269,   -1,
  550,   -1,   -1,   -1,  554,  555,   -1,  433,  279,  280,
   -1,   -1,  283,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  451,  452,  453,  454,  455,
  456,  457,  458,   -1,  460,   -1,  462,  463,  464,  465,
   -1,   -1,   -1,  314,   -1,   -1,  388,   -1,   -1,  320,
  392,  393,  394,  395,  396,  397,   -1,  399,  400,  401,
  402,  403,  404,  405,  406,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  501,  502,   -1,   -1,   -1,
  506,   -1,  508,  509,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  433,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  451,
  452,  453,  454,  455,  456,  457,  458,   -1,  460,   -1,
  462,  463,  464,  465,  550,   -1,   -1,   -1,  554,  555,
  258,   -1,  260,  261,   -1,  406,   -1,   -1,   -1,   -1,
   -1,  269,   -1,   -1,   -1,   -1,   -1,  258,   -1,  260,
  261,   -1,   -1,   -1,   -1,  283,   -1,   -1,  269,  501,
  502,   -1,  433,   -1,  506,   -1,  508,  509,  279,  280,
   -1,   -1,  283,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  451,  452,  453,  454,  455,  456,  457,  458,   -1,  460,
   -1,  462,  463,  464,  465,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  314,   -1,   -1,   -1,   -1,  550,  320,
   -1,   -1,  554,  555,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  501,  502,  503,  504,  505,  506,   -1,  508,  509,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  379,  380,  381,  382,  383,   -1,   -1,   -1,   -1,
   -1,  258,   -1,  260,  261,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  269,   -1,   -1,   -1,   -1,   -1,  406,  550,
   -1,   -1,   -1,  554,  555,   -1,  283,   -1,   -1,  258,
   -1,  260,  261,   -1,   -1,  406,   -1,   -1,   -1,   -1,
  269,   -1,   -1,   -1,   -1,  433,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  283,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  433,  451,  452,  453,  454,  455,  456,  457,
  458,   -1,  460,   -1,  462,  463,  464,  465,   -1,   -1,
  451,  452,  453,  454,  455,  456,  457,  458,   -1,  460,
   -1,  462,  463,  464,  465,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  501,  502,   -1,   -1,   -1,  506,   -1,
  508,  509,  379,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  501,  502,  503,  504,  505,  506,   -1,  508,  509,   -1,
  397,   -1,  399,   -1,  258,   -1,  260,  261,   -1,  406,
   -1,   -1,   -1,   -1,   -1,  269,   -1,  271,   -1,  273,
   -1,  258,  550,  260,  261,   -1,  554,  555,  397,  283,
  399,   -1,  269,   -1,   -1,   -1,  433,  406,   -1,  550,
   -1,   -1,   -1,  554,  555,   -1,  283,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  451,  452,  453,  454,  455,  456,
  457,  458,   -1,  460,  433,  462,  463,  464,  465,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  451,  452,  453,  454,  455,  456,  457,  458,
   -1,  460,   -1,  462,  463,  464,  465,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  501,  502,   -1,   -1,   -1,  506,
   -1,  508,  509,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  501,  502,   -1,   -1,   -1,  506,   -1,  508,
  509,   -1,   -1,   -1,   -1,  258,   -1,  260,  261,   -1,
   -1,   -1,  406,  550,   -1,   -1,  269,  554,  555,  272,
   -1,   -1,  258,   -1,  260,  261,   -1,   -1,   -1,  406,
  283,   -1,   -1,  269,   -1,   -1,   -1,   -1,   -1,  433,
   -1,  550,   -1,   -1,   -1,  554,  555,  283,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  433,  451,  452,  453,
  454,  455,  456,  457,  458,   -1,  460,   -1,  462,  463,
  464,  465,   -1,   -1,  451,  452,  453,  454,  455,  456,
  457,  458,   -1,  460,   -1,  462,  463,  464,  465,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  501,  502,   -1,
   -1,   -1,  506,   -1,  508,  509,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  501,  502,   -1,   -1,   -1,  506,
   -1,  508,  509,  258,   -1,  260,  261,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  269,  522,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  406,   -1,   -1,  550,   -1,  283,   -1,
  554,  555,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  406,   -1,   -1,  550,   -1,   -1,   -1,  554,  555,   -1,
  433,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  433,  451,  452,
  453,  454,  455,  456,  457,  458,   -1,  460,   -1,  462,
  463,  464,  465,   -1,   -1,  451,  452,  453,  454,  455,
  456,  457,  458,   -1,  460,   -1,  462,  463,  464,  465,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  258,
   -1,  260,  261,   -1,   -1,   -1,   -1,   -1,  501,  502,
  269,   -1,   -1,  506,   -1,  508,  509,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  283,  501,  502,   -1,   -1,   -1,
  506,   -1,  508,  509,  258,   -1,  260,  261,   -1,   -1,
   -1,  406,   -1,   -1,   -1,  269,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  550,   -1,  283,
   -1,  554,  555,   -1,   -1,   -1,   -1,  268,  433,  258,
   -1,  260,  261,   -1,  550,   -1,   -1,   -1,  554,  555,
  269,   -1,   -1,   -1,   -1,   -1,  451,  452,  453,  454,
  455,  456,  457,  458,  283,  460,   -1,  462,  463,  464,
  465,   -1,   -1,   -1,  258,   -1,  260,  261,   -1,   -1,
   -1,   -1,   -1,   -1,  315,  269,   -1,   -1,   -1,   -1,
  321,  322,   -1,   -1,  325,   -1,   -1,  328,   -1,  283,
   -1,   -1,   -1,  334,  335,   -1,  501,  502,   -1,   -1,
  341,  506,   -1,  508,  509,   -1,   -1,  406,  349,  350,
  351,  352,  353,   -1,  355,   -1,   -1,   -1,   -1,  360,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  433,  258,   -1,  260,  261,   -1,
   -1,   -1,  406,   -1,   -1,  550,  269,   -1,   -1,  554,
  555,   -1,  451,  452,  453,  454,  455,  456,  457,  458,
  283,  460,   -1,  462,  463,  464,  465,   -1,   -1,  433,
  258,   -1,  260,  261,   -1,   -1,   -1,  406,   -1,   -1,
   -1,  269,   -1,   -1,   -1,   -1,   -1,  451,  452,  453,
  454,  455,  456,  457,  458,  283,  460,   -1,  462,  463,
  464,  465,  501,  502,  433,   -1,   -1,  506,   -1,  508,
  509,   -1,  406,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  451,  452,  453,  454,  455,  456,  457,  458,
   -1,  460,   -1,  462,  463,  464,  465,  501,  502,  433,
   -1,   -1,  506,   -1,  508,  509,   -1,   -1,   -1,   -1,
   -1,  550,   -1,   -1,   -1,  554,  555,  451,  452,  453,
  454,  455,  456,  457,  458,   -1,  460,   -1,  462,  463,
  464,  465,  501,  502,   -1,   -1,   -1,  506,   -1,  508,
  509,   -1,   -1,  406,   -1,   -1,  550,   -1,   -1,   -1,
  554,  555,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  501,  502,   -1,
  433,   -1,  506,   -1,  508,  509,   -1,   -1,  406,   -1,
   -1,  550,   -1,   -1,   -1,  554,  555,   -1,  451,  452,
  453,  454,  455,  456,  457,  458,   -1,  460,   -1,  462,
  463,  464,  465,   -1,   -1,  433,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  550,   -1,   -1,   -1,
  554,  555,   -1,  451,  452,  453,  454,  455,  456,  457,
  458,   -1,  460,   -1,  462,  463,  464,  465,  501,  502,
   -1,   -1,   -1,  506,   -1,  508,  509,   -1,   -1,   -1,
   -1,  310,   -1,   -1,   -1,   -1,  315,   -1,   -1,  318,
  319,   -1,  321,  322,   -1,   -1,   -1,   -1,   -1,  328,
  329,   -1,   -1,  501,  502,  334,  335,   -1,  506,   -1,
  508,  509,  341,   -1,  343,  344,  345,  550,   -1,   -1,
   -1,  554,  555,  352,  353,   -1,  258,   -1,  260,   -1,
   -1,   -1,  361,  362,  266,  267,  268,  269,   -1,  368,
   -1,   -1,   -1,  275,   -1,   -1,   -1,  279,  280,   -1,
  282,   -1,  550,   -1,   -1,   -1,  554,  555,  290,   -1,
  292,  293,  294,  295,  296,   -1,  298,  299,  300,  301,
   -1,  303,  304,   -1,  306,  307,   -1,   -1,   -1,  279,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  321,
  322,  323,  324,   -1,   -1,  327,   -1,   -1,   -1,   -1,
   -1,   -1,  334,  335,   -1,   -1,  338,   -1,  340,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  349,   -1,  351,
  352,  353,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  258,  364,  260,   -1,  367,   -1,  369,   -1,  266,
  267,  268,  269,   -1,   -1,   -1,   -1,   -1,  275,   -1,
   -1,   -1,  279,  280,   -1,  282,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  290,   -1,  292,  293,  294,  295,  296,
   -1,  298,  299,  300,  301,   -1,  303,  304,   -1,  306,
  307,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  321,  322,  323,  324,   -1,   -1,
  327,   -1,   -1,   -1,   -1,   -1,   -1,  334,  335,   -1,
   -1,  338,   -1,  340,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  349,   -1,  351,  352,  353,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  364,   -1,  371,
  367,   -1,  369,   -1,   -1,   -1,   -1,   -1,  448,  449,
   -1,  451,  452,  453,  454,  455,  456,  457,  458,  459,
  460,   -1,   -1,   -1,   -1,   -1,  466,  467,  468,  469,
  470,  503,  504,  505,  474,  475,  408,  477,  478,   -1,
   -1,  413,   -1,   -1,   -1,  485,   -1,  419,  488,  489,
  490,  491,  492,  493,  494,  495,  496,  497,  498,  499,
  500,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  444,  445,   -1,   -1,  448,  449,  450,  451,
  452,  453,  454,  455,  456,  457,  458,  459,  460,   -1,
  462,  463,  464,  465,  466,  467,  468,  469,  470,  471,
  472,  473,  474,  475,  476,  477,  478,  479,  480,  258,
   -1,  260,  484,   -1,   -1,   -1,   -1,   -1,  267,  268,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  501,
   -1,   -1,   -1,   -1,   -1,   -1,  503,  504,  505,   -1,
   -1,  290,   -1,  292,  293,  294,  295,  296,   -1,  298,
  299,  300,  301,   -1,  303,  304,   -1,  306,  307,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  321,  322,  323,  324,   -1,   -1,  327,   -1,
   -1,   -1,   -1,   -1,   -1,  334,  335,   -1,   -1,  338,
   -1,  340,   -1,   -1,   -1,   -1,  258,   -1,  260,   -1,
  349,   -1,  351,  352,  353,  267,  268,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  364,   -1,   -1,  367,   -1,
  369,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  290,   -1,
  292,  293,  294,  295,  296,   -1,  298,  299,  300,  301,
   -1,  303,  304,   -1,  306,  307,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  321,
  322,  323,  324,   -1,   -1,  327,   -1,   -1,   -1,   -1,
   -1,   -1,  334,  335,   -1,   -1,  338,   -1,  340,   -1,
   -1,   -1,   -1,  258,   -1,  260,   -1,  349,   -1,  351,
  352,  353,  267,  268,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  364,   -1,   -1,  367,   -1,  369,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  290,   -1,  292,  293,  294,
  295,  296,   -1,  298,  299,  300,  301,   -1,  303,  304,
   -1,  306,  307,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  321,  322,  323,  324,
   -1,   -1,  327,   -1,   -1,   -1,   -1,   -1,   -1,  334,
  335,   -1,   -1,  338,   -1,  340,   -1,   -1,   -1,   -1,
  258,   -1,  260,   -1,  349,   -1,  351,  352,  353,  267,
  268,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  364,
   -1,   -1,  367,   -1,  369,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  290,   -1,  292,  293,  294,  295,  296,   -1,
  298,  299,  300,  301,   -1,  303,  304,   -1,  306,  307,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  321,  322,  323,  324,   -1,   -1,  327,
   -1,   -1,   -1,   -1,   -1,   -1,  334,  335,   -1,   -1,
  338,   -1,  340,   -1,   -1,   -1,   -1,  258,   -1,  260,
   -1,  349,   -1,  351,  352,  353,  267,  268,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  364,   -1,   -1,  367,
   -1,  369,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  290,
   -1,  292,  293,  294,  295,  296,   -1,  298,  299,  300,
  301,   -1,  303,  304,   -1,  306,  307,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  321,  322,  323,  324,   -1,   -1,  327,   -1,   -1,   -1,
   -1,   -1,   -1,  334,  335,   -1,   -1,  338,   -1,  340,
   -1,   -1,   -1,   -1,  258,   -1,  260,   -1,  349,   -1,
  351,  352,  353,  267,  268,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  364,   -1,   -1,  367,   -1,  369,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  290,   -1,  292,  293,
  294,  295,  296,   -1,  298,  299,  300,  301,   -1,  303,
  304,   -1,  306,  307,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  321,  322,  323,
  324,   -1,   -1,  327,   -1,   -1,   -1,   -1,   -1,   -1,
  334,  335,   -1,   -1,  338,   -1,  340,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  349,   -1,  351,  352,  353,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  364,   -1,   -1,  367,   -1,  369,
  };

#line 3447 "C:\Apps\mono\mcs\ilasm\parser\ILParser.jay"

    }

#line default
    namespace yydebug
    {
        using System;
        internal interface yyDebug
        {
            void push(int state, Object value);
            void lex(int state, int token, string name, Object value);
            void shift(int from, int to, int errorFlag);
            void pop(int state);
            void discard(int state, int token, string name, Object value);
            void reduce(int from, int to, int rule, string text, int len);
            void shift(int from, int to);
            void accept(Object value);
            void error(string message);
            void reject();
        }

        class yyDebugSimple : yyDebug
        {
            void println(string s)
            {
                Console.Error.WriteLine(s);
            }

            public void push(int state, Object value)
            {
                println("push\tstate " + state + "\tvalue " + value);
            }

            public void lex(int state, int token, string name, Object value)
            {
                println("lex\tstate " + state + "\treading " + name + "\tvalue " + value);
            }

            public void shift(int from, int to, int errorFlag)
            {
                switch (errorFlag)
                {
                    default:                // normally
                        println("shift\tfrom state " + from + " to " + to);
                        break;
                    case 0:
                    case 1:
                    case 2:     // in error recovery
                        println("shift\tfrom state " + from + " to " + to
                                + "\t" + errorFlag + " left to recover");
                        break;
                    case 3:             // normally
                        println("shift\tfrom state " + from + " to " + to + "\ton error");
                        break;
                }
            }

            public void pop(int state)
            {
                println("pop\tstate " + state + "\ton error");
            }

            public void discard(int state, int token, string name, Object value)
            {
                println("discard\tstate " + state + "\ttoken " + name + "\tvalue " + value);
            }

            public void reduce(int from, int to, int rule, string text, int len)
            {
                println("reduce\tstate " + from + "\tuncover " + to
                        + "\trule (" + rule + ") " + text);
            }

            public void shift(int from, int to)
            {
                println("goto\tfrom state " + from + " to " + to);
            }

            public void accept(Object value)
            {
                println("accept\tvalue " + value);
            }

            public void error(string message)
            {
                println("error\t" + message);
            }

            public void reject()
            {
                println("reject");
            }

        }
    }
    // %token constants
    class Token
    {
        public const int EOF = 257;
        public const int ID = 258;
        public const int QSTRING = 259;
        public const int SQSTRING = 260;
        public const int COMP_NAME = 261;
        public const int INT32 = 262;
        public const int INT64 = 263;
        public const int FLOAT64 = 264;
        public const int HEXBYTE = 265;
        public const int DOT = 266;
        public const int OPEN_BRACE = 267;
        public const int CLOSE_BRACE = 268;
        public const int OPEN_BRACKET = 269;
        public const int CLOSE_BRACKET = 270;
        public const int OPEN_PARENS = 271;
        public const int CLOSE_PARENS = 272;
        public const int COMMA = 273;
        public const int COLON = 274;
        public const int DOUBLE_COLON = 275;
        public const int SEMICOLON = 277;
        public const int ASSIGN = 278;
        public const int STAR = 279;
        public const int AMPERSAND = 280;
        public const int PLUS = 281;
        public const int SLASH = 282;
        public const int BANG = 283;
        public const int ELLIPSIS = 284;
        public const int DASH = 286;
        public const int OPEN_ANGLE_BRACKET = 287;
        public const int CLOSE_ANGLE_BRACKET = 288;
        public const int UNKNOWN = 289;
        public const int INSTR_NONE = 290;
        public const int INSTR_VAR = 291;
        public const int INSTR_I = 292;
        public const int INSTR_I8 = 293;
        public const int INSTR_R = 294;
        public const int INSTR_BRTARGET = 295;
        public const int INSTR_METHOD = 296;
        public const int INSTR_NEWOBJ = 297;
        public const int INSTR_FIELD = 298;
        public const int INSTR_TYPE = 299;
        public const int INSTR_STRING = 300;
        public const int INSTR_SIG = 301;
        public const int INSTR_RVA = 302;
        public const int INSTR_TOK = 303;
        public const int INSTR_SWITCH = 304;
        public const int INSTR_PHI = 305;
        public const int INSTR_LOCAL = 306;
        public const int INSTR_PARAM = 307;
        public const int D_ADDON = 308;
        public const int D_ALGORITHM = 309;
        public const int D_ASSEMBLY = 310;
        public const int D_BACKING = 311;
        public const int D_BLOB = 312;
        public const int D_CAPABILITY = 313;
        public const int D_CCTOR = 314;
        public const int D_CLASS = 315;
        public const int D_COMTYPE = 316;
        public const int D_CONFIG = 317;
        public const int D_IMAGEBASE = 318;
        public const int D_CORFLAGS = 319;
        public const int D_CTOR = 320;
        public const int D_CUSTOM = 321;
        public const int D_DATA = 322;
        public const int D_EMITBYTE = 323;
        public const int D_ENTRYPOINT = 324;
        public const int D_EVENT = 325;
        public const int D_EXELOC = 326;
        public const int D_EXPORT = 327;
        public const int D_FIELD = 328;
        public const int D_FILE = 329;
        public const int D_FIRE = 330;
        public const int D_GET = 331;
        public const int D_HASH = 332;
        public const int D_IMPLICITCOM = 333;
        public const int D_LANGUAGE = 334;
        public const int D_LINE = 335;
        public const int D_XLINE = 336;
        public const int D_LOCALE = 337;
        public const int D_LOCALS = 338;
        public const int D_MANIFESTRES = 339;
        public const int D_MAXSTACK = 340;
        public const int D_METHOD = 341;
        public const int D_MIME = 342;
        public const int D_MODULE = 343;
        public const int D_MRESOURCE = 344;
        public const int D_NAMESPACE = 345;
        public const int D_ORIGINATOR = 346;
        public const int D_OS = 347;
        public const int D_OTHER = 348;
        public const int D_OVERRIDE = 349;
        public const int D_PACK = 350;
        public const int D_PARAM = 351;
        public const int D_PERMISSION = 352;
        public const int D_PERMISSIONSET = 353;
        public const int D_PROCESSOR = 354;
        public const int D_PROPERTY = 355;
        public const int D_PUBLICKEY = 356;
        public const int D_PUBLICKEYTOKEN = 357;
        public const int D_REMOVEON = 358;
        public const int D_SET = 359;
        public const int D_SIZE = 360;
        public const int D_STACKRESERVE = 361;
        public const int D_SUBSYSTEM = 362;
        public const int D_TITLE = 363;
        public const int D_TRY = 364;
        public const int D_VER = 365;
        public const int D_VTABLE = 366;
        public const int D_VTENTRY = 367;
        public const int D_VTFIXUP = 368;
        public const int D_ZEROINIT = 369;
        public const int K_AT = 370;
        public const int K_AS = 371;
        public const int K_AGGRESSIVEINLINING = 372;
        public const int K_IMPLICITCOM = 373;
        public const int K_IMPLICITRES = 374;
        public const int K_NOAPPDOMAIN = 375;
        public const int K_NOPROCESS = 376;
        public const int K_NOMACHINE = 377;
        public const int K_EXTERN = 378;
        public const int K_INSTANCE = 379;
        public const int K_EXPLICIT = 380;
        public const int K_DEFAULT = 381;
        public const int K_VARARG = 382;
        public const int K_UNMANAGED = 383;
        public const int K_CDECL = 384;
        public const int K_STDCALL = 385;
        public const int K_THISCALL = 386;
        public const int K_FASTCALL = 387;
        public const int K_MARSHAL = 388;
        public const int K_IN = 389;
        public const int K_OUT = 390;
        public const int K_OPT = 391;
        public const int K_STATIC = 392;
        public const int K_PUBLIC = 393;
        public const int K_PRIVATE = 394;
        public const int K_FAMILY = 395;
        public const int K_INITONLY = 396;
        public const int K_RTSPECIALNAME = 397;
        public const int K_STRICT = 398;
        public const int K_SPECIALNAME = 399;
        public const int K_ASSEMBLY = 400;
        public const int K_FAMANDASSEM = 401;
        public const int K_FAMORASSEM = 402;
        public const int K_PRIVATESCOPE = 403;
        public const int K_LITERAL = 404;
        public const int K_NOTSERIALIZED = 405;
        public const int K_VALUE = 406;
        public const int K_NOT_IN_GC_HEAP = 407;
        public const int K_INTERFACE = 408;
        public const int K_SEALED = 409;
        public const int K_ABSTRACT = 410;
        public const int K_AUTO = 411;
        public const int K_SEQUENTIAL = 412;
        public const int K_ANSI = 413;
        public const int K_UNICODE = 414;
        public const int K_AUTOCHAR = 415;
        public const int K_BESTFIT = 416;
        public const int K_IMPORT = 417;
        public const int K_SERIALIZABLE = 418;
        public const int K_NESTED = 419;
        public const int K_LATEINIT = 420;
        public const int K_EXTENDS = 421;
        public const int K_IMPLEMENTS = 422;
        public const int K_FINAL = 423;
        public const int K_VIRTUAL = 424;
        public const int K_HIDEBYSIG = 425;
        public const int K_NEWSLOT = 426;
        public const int K_UNMANAGEDEXP = 427;
        public const int K_PINVOKEIMPL = 428;
        public const int K_NOMANGLE = 429;
        public const int K_OLE = 430;
        public const int K_LASTERR = 431;
        public const int K_WINAPI = 432;
        public const int K_NATIVE = 433;
        public const int K_IL = 434;
        public const int K_CIL = 435;
        public const int K_OPTIL = 436;
        public const int K_MANAGED = 437;
        public const int K_FORWARDREF = 438;
        public const int K_RUNTIME = 439;
        public const int K_INTERNALCALL = 440;
        public const int K_SYNCHRONIZED = 441;
        public const int K_NOINLINING = 442;
        public const int K_NOOPTIMIZATION = 443;
        public const int K_CUSTOM = 444;
        public const int K_FIXED = 445;
        public const int K_SYSSTRING = 446;
        public const int K_ARRAY = 447;
        public const int K_VARIANT = 448;
        public const int K_CURRENCY = 449;
        public const int K_SYSCHAR = 450;
        public const int K_VOID = 451;
        public const int K_BOOL = 452;
        public const int K_INT8 = 453;
        public const int K_INT16 = 454;
        public const int K_INT32 = 455;
        public const int K_INT64 = 456;
        public const int K_FLOAT32 = 457;
        public const int K_FLOAT64 = 458;
        public const int K_ERROR = 459;
        public const int K_UNSIGNED = 460;
        public const int K_UINT = 461;
        public const int K_UINT8 = 462;
        public const int K_UINT16 = 463;
        public const int K_UINT32 = 464;
        public const int K_UINT64 = 465;
        public const int K_DECIMAL = 466;
        public const int K_DATE = 467;
        public const int K_BSTR = 468;
        public const int K_LPSTR = 469;
        public const int K_LPWSTR = 470;
        public const int K_LPTSTR = 471;
        public const int K_VBBYREFSTR = 472;
        public const int K_OBJECTREF = 473;
        public const int K_IUNKNOWN = 474;
        public const int K_IDISPATCH = 475;
        public const int K_STRUCT = 476;
        public const int K_SAFEARRAY = 477;
        public const int K_INT = 478;
        public const int K_BYVALSTR = 479;
        public const int K_TBSTR = 480;
        public const int K_LPVOID = 481;
        public const int K_ANY = 482;
        public const int K_FLOAT = 483;
        public const int K_LPSTRUCT = 484;
        public const int K_NULL = 485;
        public const int K_PTR = 486;
        public const int K_VECTOR = 487;
        public const int K_HRESULT = 488;
        public const int K_CARRAY = 489;
        public const int K_USERDEFINED = 490;
        public const int K_RECORD = 491;
        public const int K_FILETIME = 492;
        public const int K_BLOB = 493;
        public const int K_STREAM = 494;
        public const int K_STORAGE = 495;
        public const int K_STREAMED_OBJECT = 496;
        public const int K_STORED_OBJECT = 497;
        public const int K_BLOB_OBJECT = 498;
        public const int K_CF = 499;
        public const int K_CLSID = 500;
        public const int K_METHOD = 501;
        public const int K_CLASS = 502;
        public const int K_PINNED = 503;
        public const int K_MODREQ = 504;
        public const int K_MODOPT = 505;
        public const int K_TYPEDREF = 506;
        public const int K_TYPE = 507;
        public const int K_WCHAR = 508;
        public const int K_CHAR = 509;
        public const int K_FROMUNMANAGED = 510;
        public const int K_CALLMOSTDERIVED = 511;
        public const int K_BYTEARRAY = 512;
        public const int K_WITH = 513;
        public const int K_INIT = 514;
        public const int K_TO = 515;
        public const int K_CATCH = 516;
        public const int K_FILTER = 517;
        public const int K_FINALLY = 518;
        public const int K_FAULT = 519;
        public const int K_HANDLER = 520;
        public const int K_TLS = 521;
        public const int K_FIELD = 522;
        public const int K_PROPERTY = 523;
        public const int K_REQUEST = 524;
        public const int K_DEMAND = 525;
        public const int K_ASSERT = 526;
        public const int K_DENY = 527;
        public const int K_PERMITONLY = 528;
        public const int K_LINKCHECK = 529;
        public const int K_INHERITCHECK = 530;
        public const int K_REQMIN = 531;
        public const int K_REQOPT = 532;
        public const int K_REQREFUSE = 533;
        public const int K_PREJITGRANT = 534;
        public const int K_PREJITDENY = 535;
        public const int K_NONCASDEMAND = 536;
        public const int K_NONCASLINKDEMAND = 537;
        public const int K_NONCASINHERITANCE = 538;
        public const int K_READONLY = 539;
        public const int K_NOMETADATA = 540;
        public const int K_ALGORITHM = 541;
        public const int K_FULLORIGIN = 542;
        public const int K_ENABLEJITTRACKING = 543;
        public const int K_DISABLEJITOPTIMIZER = 544;
        public const int K_RETARGETABLE = 545;
        public const int K_PRESERVESIG = 546;
        public const int K_BEFOREFIELDINIT = 547;
        public const int K_ALIGNMENT = 548;
        public const int K_NULLREF = 549;
        public const int K_VALUETYPE = 550;
        public const int K_COMPILERCONTROLLED = 551;
        public const int K_REQSECOBJ = 552;
        public const int K_ENUM = 553;
        public const int K_OBJECT = 554;
        public const int K_STRING = 555;
        public const int K_TRUE = 556;
        public const int K_FALSE = 557;
        public const int K_IS = 558;
        public const int K_ON = 559;
        public const int K_OFF = 560;
        public const int K_FORWARDER = 561;
        public const int K_CHARMAPERROR = 562;
        public const int K_LEGACY = 563;
        public const int K_LIBRARY = 564;
        public const int yyErrorCode = 256;
    }
    namespace yyParser
    {
        using System;
        /** thrown for irrecoverable syntax errors and stack overflow.
          */
        internal class yyException : System.Exception
        {
            public yyException(string message) : base(message)
            {
            }
        }
        internal class yyUnexpectedEof : yyException
        {
            public yyUnexpectedEof(string message) : base(message)
            {
            }
            public yyUnexpectedEof() : base("")
            {
            }
        }

        /** must be implemented by a scanner object to supply input to the parser.
          */
        internal interface yyInput
        {
            /** move on to next token.
                @return false if positioned beyond tokens.
                @throws IOException on input error.
              */
            bool advance(); // throws java.io.IOException;
            /** classifies current token.
                Should not be called if advance() returned false.
                @return current %token or single character.
              */
            int token();
            /** associated with current token.
                Should not be called if advance() returned false.
                @return value for token().
              */
            Object value();
        }
    }
} // close outermost namespace, that MUST HAVE BEEN opened in the prolog
