using NotificationSamples;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Wrapper class to work with Unity notification package (actually it will work with another ... wrapper classes from Unity notification sample : GameNotificationsManager)
/// </summary>
public class NotificationSenderWrapper : MonoSingleton<NotificationSenderWrapper> {
	#region Init, config
	[Header("Ref")]
	[SerializeField] GameNotificationsManager manager;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void OnEnable() {
		if(isFailedConfig)
			return;
	}

	void OnDisable() {
		if(isFailedConfig)
			return;
	}

	protected override void DoOnAwake() {
		base.DoOnAwake();
		if(isFailedConfig)
			return;
		base.DoOnAwake();
		SetupNotificationChannels();
	}

	void SetupNotificationChannels() {
		var c1 = new GameNotificationChannel(ChannelID.DEFAULT, "Default Game Channel", "Generic notifications");
		var c2 = new GameNotificationChannel(ChannelID.NEWS, "News Channel", "News feed notifications");
		var c3 = new GameNotificationChannel(ChannelID.REMINDER, "Reminder Channel", "Reminder notifications");
		manager.Initialize(c1, c2, c3);
	}
	#endregion// Init config


	#region Public
	/// <summary>
	/// Queue a notification with the given parameters.
	/// </summary>
	/// <param name="title">The title for the notification.</param>
	/// <param name="body">The body text for the notification.</param>
	/// <param name="deliveryTime">The time to deliver the notification.</param>
	/// <param name="badgeNumber">The optional badge number to display on the application icon.</param>
	/// <param name="reschedule">
	/// Whether to reschedule the notification if foregrounding and the notification hasn't yet been shown.
	/// </param>
	/// <param name="channelId">Channel ID to use. If this is null/empty then it will use the default ID. For Android
	/// the channel must be registered in <see cref="GameNotificationsManager.Initialize"/>.</param>
	/// <param name="smallIcon">Notification small icon.</param>
	/// <param name="largeIcon">Notification large icon.</param>
	public void SendNotification(string title, string body, DateTime deliveryTime, int? badgeNumber = null,
		bool reschedule = false, string channelId = null, string smallIcon = null, string largeIcon = null) {
		if(isFailedConfig)
			return;
		// create notification via manager
		var notification = manager.CreateNotification();
		if(notification == null) {
			return;
		}
		// config content
		notification.Title = title;
		notification.Body = body;
		notification.Group = !string.IsNullOrEmpty(channelId) ? channelId : ChannelID.DEFAULT;
		notification.DeliveryTime = deliveryTime;
		notification.SmallIcon = smallIcon;
		notification.LargeIcon = largeIcon;
		if(badgeNumber != null) {
			notification.BadgeNumber = badgeNumber;
		}
		// schedule
		var notificationToDisplay = manager.ScheduleNotification(notification);
		notificationToDisplay.Reschedule = reschedule;
		//updatePendingNotifications = true;
		//QueueEvent($"Queued event with ID \"{notification.Id}\" at time {deliveryTime:HH:mm}");
	}
	#endregion//Public


	#region Private
	//private void Update() {
	//	if(updatePendingNotifications) {
	//		UpdatePendingNotifications();
	//	}
	//}

	//private void UpdatePendingNotifications() {
	//	Debug.Log("Updating pending list.");

	//	updatePendingNotifications = false;

	//	// Clear all existing ones
	//	foreach(object child in pendingNotificationsListParent.transform) {
	//		if(child is Transform childTransform) {
	//			Destroy(childTransform.gameObject);
	//		}
	//	}

	//	if(manager?.PendingNotifications == null)
	//		return;

	//	// Recreate based on currently pending list
	//	// Note: Using ToArray because the list can change during the loop.
	//	foreach(PendingNotification scheduledNotification in manager.PendingNotifications.ToArray()) {
	//		PendingNotificationItem newItem =
	//			Instantiate(pendingNotificationPrefab, pendingNotificationsListParent);
	//		newItem.Show(scheduledNotification, this);
	//	}
	//}
	//private void QueueEvent(string eventText) {
	//    Debug.Log($"Queueing event with text \"{eventText}\"");
	//    NotificationEventItem newItem = Instantiate(eventPrefab, pendingEventParent);
	//    newItem.Show(eventText);
	//}
	#endregion//Private


	#region Classes

	// On iOS, this represents the notification's Category Identifier, and is optional
	// On Android, this represents the notification's channel, and is required (at least one).
	// Channels defined as global constants so can be referred to from GameController.cs script when setting/sending notification
	public class ChannelID {
		public const string DEFAULT = "Game";
		public const string REMINDER = "Reminder";
		public const string NEWS = "News";
		//const string ChannelId = "game_channel0";
		//const string ReminderChannelId = "reminder_channel1";
		//const string NewsChannelId = "news_channel2";
	}
	#endregion//Classes
}
