namespace XN
{
    public static class ViewCarTitleItemSystem
    {
        #region CircleLife

        public static void OnRefresh(this ViewCarTitleItem self, ViewCarTitleItemData data)
        {
            self.transform.localPosition = data.LocalPosition;
            self.UITitleTextMeshPro.text = data.Title;
            YooAssetManager.Instance.LoadSpriteAsync(data.Frame, self.UIFrameSpriteRenderer);
        }

        #endregion

        #region UIEvents

        #endregion

        #region GlobalEvents

        #endregion

        #region Logics

        #endregion
    }
}