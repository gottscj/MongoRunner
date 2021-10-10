using System.Collections.Generic;
using Mongo2Go.Helper;

namespace MongoRunner
{
    public class LocalMongoDbProcess : IMongoDbProcess
    {

        private WrappedProcess _process;

        public IEnumerable<string> ErrorOutput { get; set; }
        public IEnumerable<string> StandardOutput { get; set; }

        public LocalMongoDbProcess(WrappedProcess process)
        {
            _process = process;
        }

        public void Dispose()
        {
            _process?.Dispose();
        }
    }
}