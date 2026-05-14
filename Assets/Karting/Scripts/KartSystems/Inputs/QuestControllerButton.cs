using UnityEngine.XR;

namespace KartGame.KartSystems
{
    public enum QuestControllerButton
    {
        None = -1,
        Trigger = 0,
        Grip = 1,
        PrimaryButton = 2,
        SecondaryButton = 3,
        Primary2DAxisClick = 4
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
                case QuestControllerButton.None:
                    return false;
                case QuestControllerButton.Trigger:
                    return (device.TryGetFeatureValue(CommonUsages.trigger, out float trigger) && trigger > analogThreshold) ||
                           (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed) && triggerPressed);
                case QuestControllerButton.Grip:
                    return (device.TryGetFeatureValue(CommonUsages.grip, out float grip) && grip > analogThreshold) ||
                           (device.TryGetFeatureValue(CommonUsages.gripButton, out bool gripPressed) && gripPressed);
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
