namespace tiantang_auto_harvest.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// 向Base64编码的字符串补全末尾的等号，如不需补全则不补全
        /// </summary>
        /// <param name="str">Base64编码过的字符串</param>
        /// <returns>补全过等号的Base64字符串</returns>
        public static string PaddingBase64String(this string str)
        {
            return str.PadRight(str.Length + (str.Length * 3) % 4, '=');
        }
    }
}
