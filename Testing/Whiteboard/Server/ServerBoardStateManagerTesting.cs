using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Whiteboard;
using Moq;

namespace Testing.Whiteboard.Server
{
    [TestFixture]
    class ServerBoardStateManagerTesting
    {
        private ServerBoardStateManager _serverBoardStateManager;
        private Mock<IServerCheckPointHandler> _fakeServerCheckpointHandler;

        [SetUp]
        public void SetUp()
        {
            
        }
    }
}
