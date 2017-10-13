using UnityEngine;
using System;
using System.Collections.Generic;

public enum NotificationLevel
{
	IMPORTANT,
	INFO,
	NONE
}

public class Notification
{
	private NotificationLevel _level;
	public NotificationLevel Level 
	{ get { return _level; } }
	
	private string _title;
	public string Title 
	{ get { return _title; } }
	
	private string _content;
	public string Content 
	{ get { return _content; } }
	
	private DateTime _creationTime;
	public DateTime CreationTime
	{ get { return _creationTime; } }
	
	private Texture _icon;
	public Texture Icon
	{ get { return _icon; } }

    public int _activeTimeMilliseconds;
    public int ActiveTimeMilliseconds
    { get { return _activeTimeMilliseconds; } }

    public Notification(NotificationLevel level, Texture icon, string title, string content, int activeTimeMilliseconds)
	{
		_level = level;
		_title = title;
		_content = content;
		_icon = icon;
		_creationTime = DateTime.Now;
        _activeTimeMilliseconds = activeTimeMilliseconds;
    }
}

public class DoNotify : MonoBehaviour {
	
	private GUIStyle _titleStyle;
	private GUIStyle _contentStyle;
	
	private List<Notification> _notifications;
	
	public Texture importantTex;
	public Texture infoTex;

    void Start () {
		_notifications = new List<Notification> ();
		
		_titleStyle = new GUIStyle ();
		_titleStyle.fontStyle = FontStyle.Bold;
		_titleStyle.normal.textColor = Color.white;
	}
	
	
	void Update () {
		DateTime now = DateTime.Now;
		List<Notification> rmv = new List<Notification> ();
		foreach (Notification n in _notifications) 
		{
			if (now > n.CreationTime.AddMilliseconds((double) n.ActiveTimeMilliseconds))
			{
				rmv.Add(n);
			}
		}
		foreach (Notification n in rmv) 
		{
			_notifications.Remove(n);
		}
	}
	
	void displayNotification(Notification notification, int left, int top)
	{
		GUI.Box (new Rect ( left - 5, top - 5, 200, 35), "");
		GUI.DrawTexture (new Rect (left, top, 24, 25), notification.Icon);
		GUI.Label (new Rect (left + 30, top, 400, 200), notification.Title, _titleStyle);
		GUI.Label (new Rect (left + 30, top + 10, 400, 200), notification.Content);
	}
	
	void OnGUI()
	{
		int i = 10;
        try
        {
            foreach (Notification n in _notifications)
            {
                displayNotification(n, Screen.width / 2 - 100, i);
                i += 40;
            }
        }
        catch (Exception)
        {

            // ignore
        }
	}
	
	public void notifySend(NotificationLevel level, string title, string content, int activeTimeMilliseconds)
	{
		Texture t = new Texture();
		if (level == NotificationLevel.IMPORTANT) 
		{
			t = importantTex;	
		} else {
			t = infoTex;
		}


		Notification n = new Notification (level, t, title, content, activeTimeMilliseconds);
		_notifications.Add (n);
	}
}