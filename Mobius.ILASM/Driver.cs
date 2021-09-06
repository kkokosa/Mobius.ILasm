using Mobius.ILasm.infrastructure;
using Mobius.ILasm.interfaces;
using Mono.ILASM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILAsmException = Mobius.ILasm.infrastructure.ILAsmException;

namespace Mobius.ILasm.Core
{
    //TODO - Search for all TODO references before starting code again. 
    //This repo contains code where references of Report.cs are being removed
    //and instead being changed to either the logger or FileProcessor.
    public class Driver
    {
        public enum Target
        {
            Dll,
            Exe
        }

        private Target target = Target.Exe;

        private bool show_tokens = false;

        // private bool show_method_def = false;
        // private bool show_method_ref = false;
        private bool show_parser = false;
        private bool scan_only = false;
        private bool debugging_info = false;
        private bool keycontainer = false;
        private string keyname;

        private CodeGen codegen;
        private readonly ILogger logger;
        private Dictionary<string, string> errors;
#if HAS_MONO_SECURITY
    			private StrongName sn;
#endif
        bool noautoinherit;

        public Driver(ILogger logger, Target target, bool showParser, bool debuggingInfo, bool showTokens)
        {
            this.logger = logger;
            this.errors = new Dictionary<string, string>();
            this.target = target;
            this.show_parser = showParser;
            this.debugging_info = debuggingInfo;
            this.show_tokens = showTokens;
        }

        public bool Assemble(string[] inputs, MemoryStream outputStream)
        {
            return Assemble(inputs.Select(ConvertToStream).ToArray(), outputStream);
        }

        public bool Assemble(MemoryStream[] inputStreams, MemoryStream outputStream)
        {
            var savedCulture = System.Threading.Thread.CurrentThread.CurrentCulture;            
            try {
                System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
                // TODO: improve error reporting:
                //  - return true/false
                //  - expose Errors property as a list (filename, errormessage)
                if (!Run(inputStreams, outputStream))
                    return false;
                //Report.Message("Operation completed successfully");
                logger.Info("Operation completed successfully");
                return true;
            }
            finally {
                System.Threading.Thread.CurrentThread.CurrentCulture = savedCulture;
            }
        }

        public bool Run(string[] inputs, MemoryStream outputStream)
        {
            return Run(inputs.Select(ConvertToStream).ToArray(), outputStream);
        }

        public bool Run(MemoryStream[] inputStreams, MemoryStream outputStream)
        {
            //Call the assembler without any arguments, results in console output of it's usage information
            //if (il_file_list.Count == 0)
            //    Usage();
            //TODO needs to go as we will be using a Stream instead of a FileStream going forward.
            //if (output_file == null)
            //    output_file = CreateOutputFilename();
            try
            {
                codegen = new CodeGen(logger, "", outputStream, target == Target.Dll, debugging_info, noautoinherit,
                    errors);
                foreach (MemoryStream inputStream in inputStreams)
                {
                    //TODO: The filepath needs to go as we will be using stream
                    //but we need a mechanism to keep information about every stream
                    // FileProcessor.FilePath = file_path;
                    Process(inputStream);
                }

                if (scan_only)
                    return true;

                if (errors.Any())
                    return false;

                if (target != Target.Dll && !codegen.HasEntryPoint)
                {
                    logger.Error("No entry point found.");
                    errors[nameof(Driver)] = "No entry point found.";
                }

                // if we have a key and aren't assembling a netmodule
                if ((keyname != null) && !codegen.IsThisAssembly(null))
                {
#if HAS_MONO_SECURITY
    						LoadKey ();
    						// this overrides any attribute or .publickey directive in the source
    						codegen.ThisAssembly.SetPublicKey (sn.PublicKey);
#else
                    throw new NotSupportedException();
#endif
                }

                try
                {
                    codegen.Write();
                }
                catch
                {
                    //TODO: How and if we need to handle this?
                    //File.Delete(output_file);
                    throw;
                }
            }
            catch (ILAsmException e)
            {
                logger.Error(e.ToString());
                return false;
            }
            catch (PEAPI.PEFileException pe)
            {
                logger.Error("Error : " + pe.Message);
                return false;
            }

#if HAS_MONO_SECURITY
                                    try {
    					if (sn != null) {
    						Report.Message ("Signing assembly with the specified strongname keypair");
    						return Sign (output_file);
    					}
                                    } catch {
                                            return false;
                                    }
#endif

            return true;
        }

#if HAS_MONO_SECURITY
    			private void LoadKey ()
    			{
    				if (keycontainer) {
    					CspParameters csp = new CspParameters ();
    					csp.KeyContainerName = keyname;
    					RSACryptoServiceProvider rsa = new RSACryptoServiceProvider (csp);
    					sn = new StrongName (rsa);
    				} else {
    					byte[] data = null;
    					using (FileStream fs = File.OpenRead (keyname)) {
    						data = new byte [fs.Length];
    						fs.Read (data, 0, data.Length);
    						fs.Close ();
    					}
    					sn = new StrongName (data);
    				}
    			}

    			private bool Sign (string filename)
    			{
    				// note: if the file cannot be signed (no public key in it) then
    				// we do not show an error, or a warning, if the key file doesn't 
    				// exists
    				return sn.Sign (filename);
    			}
#endif

        private void Process(MemoryStream inputStream)
        {
            //TODO figure out how to log with the correct IL input filename
            //logger.Info($"Assembling '{file_path}' , {FileProcessor.GetListing(null)}, to {target_string} --> '{output_file}'");

            StreamReader reader = new StreamReader(inputStream);
            ILTokenizer scanner = new ILTokenizer(reader);
            if (show_tokens)
                scanner.NewTokenEvent += new NewTokenEvent(ShowToken);
            //if (show_method_def)
            //        MethodTable.MethodDefinedEvent += new MethodDefinedEvent (ShowMethodDef);
            //if (show_method_ref)
            //       MethodTable.MethodReferencedEvent += new MethodReferencedEvent (ShowMethodRef);

            if (scan_only)
            {
                ILToken tok;
                while ((tok = scanner.NextToken) != ILToken.EOF)
                {
                    logger.Info(tok.ToString());
                }

                return;
            }

            ILParser parser = new ILParser(codegen, scanner, this.logger, errors);
            //codegen.BeginSourceFile(file_path);
            try
            {
                if (show_parser)
                    parser.yyparse(new ScannerAdapter(scanner),
                        new Mono.ILASM.yydebug.yyDebugSimple());
                else
                    parser.yyparse(new ScannerAdapter(scanner), null);
            }
            catch (ILTokenizingException ilte)
            {
                logger.Error(ilte.Location, "syntax error at token '" + ilte.Token + "'");
                throw;
            }
            catch (Mono.ILASM.yyParser.yyException ye)
            {
                logger.Error(scanner.Reader.Location, ye.Message);
                throw;
            }
            catch (ILAsmException ie)
            {
                // ie.FilePath = file_path;
                ie.Location = scanner.Reader.Location;
                throw;
            }
            catch (Exception)
            {
                // Console.Write("{0} ({1}, {2}): ", file_path, scanner.Reader.Location.line, scanner.Reader.Location.column);
                // Console.Write("{1}, {2}): ", scanner.Reader.Location.line, scanner.Reader.Location.column);
                throw;
            }
            finally
            {
                codegen.EndSourceFile();
            }
        }

        public void ShowToken(object sender, NewTokenEventArgs args)
        {
            Console.WriteLine("token: '{0}'", args.Token);
        }

        private MemoryStream ConvertToStream(string text)
        {
            byte[] byteArray = Encoding.ASCII.GetBytes(text);
            return new MemoryStream(byteArray);
        }

        /*
        public void ShowMethodDef (object sender, MethodDefinedEventArgs args)
        {
                Console.WriteLine ("***** Method defined *****");
                Console.WriteLine ("-- signature:   {0}", args.Signature);
                Console.WriteLine ("-- name:        {0}", args.Name);
                Console.WriteLine ("-- return type: {0}", args.ReturnType);
                Console.WriteLine ("-- is in table: {0}", args.IsInTable);
                Console.WriteLine ("-- method atts: {0}", args.MethodAttributes);
                Console.WriteLine ("-- impl atts:   {0}", args.ImplAttributes);
                Console.WriteLine ("-- call conv:   {0}", args.CallConv);
        }

        public void ShowMethodRef (object sender, MethodReferencedEventArgs args)
        {
                Console.WriteLine ("***** Method referenced *****");
                Console.WriteLine ("-- signature:   {0}", args.Signature);
                Console.WriteLine ("-- name:        {0}", args.Name);
                Console.WriteLine ("-- return type: {0}", args.ReturnType);
                Console.WriteLine ("-- is in table: {0}", args.IsInTable);
        }
        */

        private void AssembleFile(string file, string listing,
            string target, string output)
        {
            logger.Info($"Assembling '{file}' , {GetListing(listing)}, to {target} --> '{output}'");
        }

        private static string GetListing(string listing)
        {
            if (listing == null)
                return "no listing file";
            return listing;
        }

    }
}
