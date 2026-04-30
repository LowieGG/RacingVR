using UnityEngine.XR;

namespace KartGame.KartSystems
{
    public enum QuestControllerButton
    {
        Trigger,
        Grip,
        PrimaryButton,
        SecondaryButton,
        Primary2DAxisClick
    }

    public static class QuestControllerButtonUtility
    {
        public static bool IsPressed(InputDevice device, QuestControllerButton button, float analogThreshold)
        {
            if (!device.isValid)
            {
                return false;
            }

            switch (button)
            {
                case QuestControllerButton.Trigger:
                    return device.TryGetFeatureValue(CommonUsages.trigger, out float trigger) && trigger > analogThreshold;
                case QuestControllerButton.Grip:
                    return device.TryGetFeatureValue(CommonUsages.grip, out float grip) && grip > analogThreshold;
                case QuestControllerButton.PrimaryButton:
                    return device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primary) && primary;
                case QuestControllerButton.SecondaryButton:
                    return device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondary) && secondary;
                case QuestControllerButton.Primary2DAxisClick:
                    return device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool axisClick) && axisClick;
                default:
                    return false;
            }
        }
    }
}
