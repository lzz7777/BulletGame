namespace XN
{
    public static class ViewLoopListTestItemSystem
    {
        #region CircleLife

        public static void OnRefresh(this ViewLoopListTestItem self, ViewLoopListTestItemData data)
        {
            self.TitleTMP_UGUI.text = $"{data.Index} - {data.Title}";
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