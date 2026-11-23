///<summary> Screen-pixel height to keep free at the bottom for the anchored banner ad.
/// Written by the ads code (0 when the user removed ads), read by SafeAreaAdjuster with reserveBottomForBanner.
/// Lives in StandardAssets so UI code does not depend on the ads assembly </summary>
public static class BannerSpace {
	public static float BottomPixels;
}
