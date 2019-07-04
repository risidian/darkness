package md548397ecb25fb49bdc9242ceeeea3ad20;


public class SharedButtons
	extends android.app.Activity
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("Darkness.Android.SharedButtons, Darkness.Android", SharedButtons.class, __md_methods);
	}


	public SharedButtons ()
	{
		super ();
		if (getClass () == SharedButtons.class)
			mono.android.TypeManager.Activate ("Darkness.Android.SharedButtons, Darkness.Android", "", this, new java.lang.Object[] {  });
	}

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
