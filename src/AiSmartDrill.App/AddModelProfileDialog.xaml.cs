using System.Windows;

namespace AiSmartDrill.App;

/// <summary>
/// 「增加 AI 模型」对话框：收集 ApiKey、ModelName、BaseUrl 与 EnableThinking 后由调用方写入配置。
/// </summary>
public partial class AddModelProfileDialog : Window
{
    /// <summary>
    /// 用户填写的显示名称。
    /// </summary>
    public string ProfileDisplayName => TbDisplayName.Text.Trim();

    /// <summary>
    /// 用户填写的 API 密钥。
    /// </summary>
    public string ProfileApiKey => TbApiKey.Text.Trim();

    /// <summary>
    /// 用户填写的接入点 / 模型名。
    /// </summary>
    public string ProfileModelName => TbModelName.Text.Trim();

    /// <summary>
    /// 用户填写的 Base URL。
    /// </summary>
    public string ProfileBaseUrl => TbBaseUrl.Text.Trim();

    /// <summary>
    /// 是否启用深度思考。
    /// </summary>
    public bool ProfileEnableThinking => CbEnableThinking.IsChecked == true;

    /// <summary>
    /// 初始化对话框。
    /// </summary>
    public AddModelProfileDialog()
    {
        InitializeComponent();
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ProfileApiKey))
        {
            MessageBox.Show("API Key 不能为空。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(ProfileModelName))
        {
            MessageBox.Show("接入点 / 模型名不能为空。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(ProfileBaseUrl))
        {
            MessageBox.Show("Base URL 不能为空。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
