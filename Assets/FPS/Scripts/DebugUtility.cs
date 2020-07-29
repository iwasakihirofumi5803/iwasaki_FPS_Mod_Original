using UnityEngine;

public static class DebugUtility
{
    public static void HandleErrorIfNullGetComponent<TO, TS>(Component component, Component source, GameObject onObject)
    {
#if UNITY_EDITOR
        if (component == null)
        {
            Debug.LogError("エラー：タイプのコンポーネント " + typeof(TS) + " ゲームオブジェクト上 " + source.gameObject.name +
                " タイプのコンポーネントを見つけることが期待されます " + typeof(TO) + " ゲームオブジェクト上 " + onObject.name + ", しかし、何も見つかりませんでした.");
        }
#endif
    }

    public static void HandleErrorIfNullFindObject<TO, TS>(UnityEngine.Object obj, Component source)
    {
#if UNITY_EDITOR
        if (obj == null)
        {
            Debug.LogError("エラー：タイプのコンポーネント " + typeof(TS) + " ゲームオブジェクト上 " + source.gameObject.name +
                " タイプのオブジェクトを見つけることが期待されます " + typeof(TO) + " シーンにはありましたが、何も見つかりませんでした。");
        }
#endif
    }

    public static void HandleErrorIfNoComponentFound<TO, TS>(int count, Component source, GameObject onObject)
    {
#if UNITY_EDITOR
        if (count == 0)
        {
            Debug.LogError("エラー：タイプのコンポーネント " + typeof(TS) + " ゲームオブジェクト上 " + source.gameObject.name +
                " タイプのコンポーネントが少なくとも1つ見つかると予想されます " + typeof(TO) + " ゲームオブジェクト上 " + onObject.name + ", しかし、何も見つかりませんでした。");
        }
#endif
    }

    public static void HandleWarningIfDuplicateObjects<TO, TS>(int count, Component source, GameObject onObject)
    {
#if UNITY_EDITOR
        if (count > 1)
        {
            Debug.LogWarning("警告：タイプのコンポーネント " + typeof(TS) + " ゲームオブジェクト上 " + source.gameObject.name +
                " タイプのコンポーネントが1つだけ見つかると予想される " + typeof(TO) + " ゲームオブジェクト上 " + onObject.name + ", しかし、いくつかが見つかりました。 最初に見つかったものが選択されます。");
        }
#endif
    }
}
