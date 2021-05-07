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

        public FileProcessor()
        {
            error_count = 0;
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

        public static string GetListing(string listing)
        {
            if (listing == null)
                return "no listing file";
            return listing;
        }

    }
}
