namespace ByteDance.CloudSync
{
    internal interface IMatchAPIListener
    {
        void OnPodCustomMessage(ApiPodMessageData msgData);

        void OnCommandMessage(ApiMatchCommandMessage msg);
    }
}