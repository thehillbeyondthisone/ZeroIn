using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Serialization;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace AutomatonRoamba.IPCMessages
{
    [AoContract((int)IPCOpcode.Enabled)]
    public class EnabledIPCMessage : IPCMessage
    {
        public override short Opcode => (short)IPCOpcode.Enabled;

        [AoMember(0)]
        public bool SetEnabled { get; set; }

        [AoMember(1, SerializeSize = ArraySizeType.Byte)]
        public string RoamPath { get; set; }

        [AoMember(2)]
        public PathConfig PathConfig { get; set; }
    }
}
