using Mono.ILASM;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mobius.ILasm.infrastructure
{
    public class ILAsmException : Exception
    {

        string message;
        string file_path;
        Location location;

        public ILAsmException(string file_path, Location location, string message)
        {
            this.file_path = file_path;
            this.location = location;
            this.message = message;
        }

        public ILAsmException(Location location, string message)
                : this(null, location, message)
        {
        }

        public ILAsmException(string message)
                : this(null, null, message)
        {
        }

        public override string Message
        {
            get { return message; }
        }

        public Location Location
        {
            get { return location; }
            set { location = value; }
        }

        public string FilePath
        {
            get { return file_path; }
            set { file_path = value; }
        }

        public override string ToString()
        {
            string location_str = " : ";
            if (location != null)
                location_str = " (" + location.line + ", " + location.column + ") : ";

            return String.Format("{0}{1}Error : {2}",
                    (file_path != null ? file_path : ""), location_str, message);
        }

    }
}
