using Mobius.ILasm.interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mobius.ILasm.infrastructure
{
    public abstract class FileProcessor
    {
        private static int error_count;
        /* Current file being processed */
        private static string file_path;
        private readonly ILog logger;

        public FileProcessor(ILoggerFactory loggerFactory)
        {
            error_count = 0;
            logger = loggerFactory.Create();
        }

        public static string FilePath
        {
            get { return file_path; }
            set { file_path = value; }
        }

        public static int ErrorCount
        {
            get { return error_count; }
            set { error_count = value; }
        }

        //public static void AssembleFile(string file, string listing,
        //                          string target, string output)
        //{
        //    logger.Info($"Assembling '{file}' , {GetListing(listing)}, to {target} --> '{output}'");
        //}

        public static string GetListing(string listing)
        {
            if (listing == null)
                return "no listing file";
            return listing;
        }

    }
}
