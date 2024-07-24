using SNetwork;

namespace ExtraToolCustomization.Networking.MineDeployer
{
    internal class MineDeployerSync : SyncedEventMasterOnly<MineDeployerID>
    {
        public override string GUID => "MineID";

        protected override void Receive(MineDeployerID packet)
        {
            if (!packet.source.GetPlayer(out SNet_Player source)) return;

            MineDeployerManager.Internal_ReceiveMineDeployerPacket(source.Lookup, packet);
        }
    }
}
