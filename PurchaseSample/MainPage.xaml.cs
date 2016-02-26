using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Store;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PurchaseSample
{
	public sealed partial class MainPage : Page
	{
		public MainPage()
		{
			this.InitializeComponent();
			this.DataContext = this;
		}

		public ObservableCollection<string> Log { get; } = new ObservableCollection<string>();

		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			Log.Add("ジャブジャブ課金");

#if DEBUG
			// デバッグ時は CurrentAppSimulator にxmlを突っ込んで課金をシミュレート
			var proxyFile = await Package.Current.InstalledLocation.GetFileAsync("test.xml");
			await CurrentAppSimulator.ReloadSimulatorAsync(proxyFile);
			var listing = await CurrentAppSimulator.LoadListingInformationAsync();
#else
			var listing = await CurrentApp.LoadListingInformationAsync();
#endif
			var paidStage1 = listing.ProductListings["jab1"];

			// 本来はメッセージボックス等で確認を取った方がいいよね
			Log.Add($"{paidStage1.FormattedPrice} 円の課金を開始しますよ");

			await BuyAndFulFill("jab1");
		}

		private async Task BuyAndFulFill(string productId)
		{
			try
			{
#if DEBUG
				var result = await CurrentAppSimulator.RequestProductPurchaseAsync(productId);
#else
				var result = await CurrentApp.RequestProductPurchaseAsync(productId);
#endif

				Log.Add($"BuyAndFulFill {result.Status}");

				switch (result.Status)
				{
				case ProductPurchaseStatus.Succeeded: // 買った
				case ProductPurchaseStatus.NotFulfilled: // まだサーバー（？）とのやり取りが終わってないけどとりあえず買った？
					課金処理(result.TransactionId);
					FulfillProduct(productId, result.TransactionId);
					break;
				case ProductPurchaseStatus.NotPurchased: // 買ってない
					break;
				}
			}
			catch (Exception exp)
			{
				Log.Add(exp.Message);
			}
		}

		private void 課金処理(Guid transactionId)
		{
			// transactionIdが被ってなかったら課金後の処理（アイテム増加とか）を実行
		}

		private async void FulfillProduct(string productId, Guid transactionId)
		{
			var result = await CurrentAppSimulator.ReportConsumableFulfillmentAsync(productId, transactionId);
			Log.Add($"FulfillProduct {result}");
			switch (result)
			{
			case FulfillmentResult.Succeeded:
				Log.Add("課金してくれてありがとう");
				break;
			case FulfillmentResult.NothingToFulfill:
				break;
			case FulfillmentResult.PurchasePending:
				break;
			case FulfillmentResult.PurchaseReverted:    // 購入したんだけどキャンセル？
				Log.Add("課金情報をキャンセルしとく");
				break;
			case FulfillmentResult.ServerError:
				break;
			}
		}


		/// <summary>
		/// 決済処理が終わっていないやつをまとめて決済・・・？
		/// </summary>
		public async void GetUnfulfilledConsumables()
		{
#if DEBUG
			var products = await CurrentAppSimulator.GetUnfulfilledConsumablesAsync();
#else
			var products = await CurrentApp.GetUnfulfilledConsumablesAsync();
#endif

			foreach (var product in products.Where(product => product.ProductId == "jab1"))
			{
				this.課金処理(product.TransactionId);
				this.FulfillProduct(product.ProductId, product.TransactionId);
			}
		}

	}
}
