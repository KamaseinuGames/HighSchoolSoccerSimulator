// 選手のプロフィール情報を管理するクラス
[System.Serializable]
public class PlayerProfile
{
    public int uniformId;  // ユニフォーム番号
    public string nameStr; // 選手名

    public PlayerProfile(int _uniformId, string _nameStr)
    {
        this.uniformId = _uniformId;
        this.nameStr = _nameStr;
    }
}
