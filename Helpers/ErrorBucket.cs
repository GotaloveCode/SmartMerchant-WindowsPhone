using System;
using System.Collections.Generic;
using System.Text;

namespace SmartMerchant
{
    // holds a set of errors that we can build up as work through a process...
    public class ErrorBucket
    {
        private List<string> Errors { get; set; }

        // holds a fatal error reference...
        public Exception Fatal { get; internal set; }

        public ErrorBucket()
        {
            Errors = new List<string>();
        }

        // special constructor for fatal exceptions...
        private ErrorBucket(Exception ex)
            : this()
        {
            Fatal = ex;
        }

        // special constructor for cloning another error bucket...
        protected ErrorBucket(ErrorBucket donor)
            : this()
        {
            CopyFrom(donor);
        }

        public void CopyFrom(ErrorBucket donor)
        {
            // copy the normal errors...
            Errors.Clear();
            Errors.AddRange(donor.Errors);

            // copy the fatal error...
            Fatal = donor.Fatal;
        }
        public void AddError(string error)
        {
            Errors.Add(error);
        }
        public void ClearErrors()
        {
            Errors.Clear();
        }

        public void Copy(List<string> errorlist)
        {
            // copy the normal errors...
            Errors.Clear();
            Errors.AddRange(errorlist);

        }
        public bool HasErrors
        {
            get
            {
                return Errors.Count > 0 || HasFatal;
            }
        }

        public bool HasFatal
        {
            get
            {
                return Fatal != null;
            }
        }

        public string GetErrorsAsString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (string error in Errors)
            {
                if (builder.Length > 0)
                    builder.Append("\r\n");
                builder.Append(error);
            }

            // fatal?
            if (HasFatal)
            {
                if (builder.Length > 0)
                    builder.Append("\r\n-------------------------\r\n");

                // make this prettier (well, take some of the detail out so it's not so overwhelming. 
                // ideally you should log this information somewhere...
                List<Exception> exes = new List<Exception>();

                // if we have an aggregate exception, flatten it, otherwise seed the set...
                if (Fatal is AggregateException)
                    exes.Add(((AggregateException)this.Fatal).Flatten());
                else
                    exes.Add(Fatal);

                // walk...
                int index = 0;
                while (index < exes.Count)
                {
                    if (exes[index].InnerException != null)
                        exes.Add(exes[index].InnerException);

                    // add...
                    if (index > 0)
                        builder.Append("\r\n");
                    builder.Append(exes[index].Message);

                    // next...
                    index++;
                }
            }

            // return...
            return builder.ToString();
        }

        internal static ErrorBucket CreateFatalBucket(Exception ex)
        {
            return new ErrorBucket(ex);
        }

        public void AssertNoErrors()
        {
            if (HasErrors)
                throw new InvalidOperationException(string.Format("Errors have occurred:\r\n{0}", GetErrorsAsString()));
        }

    }
}