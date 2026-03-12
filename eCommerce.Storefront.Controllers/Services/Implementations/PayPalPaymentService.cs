using System;
using System.Collections.Specialized;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using eCommerce.Storefront.Controllers.Models;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using eCommerce.Storefront.Model.Orders;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace eCommerce.Storefront.Controllers.Services.Implementations
{
    public class PayPalPaymentService : IPaymentService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public PayPalPaymentService(IHttpContextAccessor httpContextAccessor, 
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public PaymentPostData GeneratePostDataFor(OrderPaymentRequest orderRequest)
        {
            var paymentPostData = new PaymentPostData();
            var postDataAndValue = new NameValueCollection();

            paymentPostData.PostDataAndValue = postDataAndValue;
            // When a real PayPal account is used, the form should be sent to 
            // https://www.paypal.com/cgi-bin/webscr.
            // For testing use "https://www.sandbox.paypal.com/cgi-bin/webscr"
            paymentPostData.PaymentPostToUrl = _configuration["PayPalPaymentPostToUrl"];
            
            // For shopping cart purchases.
            postDataAndValue.Add("cmd", "_cart");
            // Indicates the use of third- party shopping cart.
            postDataAndValue.Add("upload", "1");
            // This is the seller’s e-mail address. 
            // You must supply your own address here!!!
            postDataAndValue.Add("business", _configuration["PayPalBusinessEmail"]);
            // This field does not take part in the shopping process. 
            // It simply will be passed to the IPN script at the time 
            // of transaction confirmation.
            postDataAndValue.Add("custom", orderRequest.Id.ToString());
            // This parameter represents the default currency code. 
            postDataAndValue.Add("currency_code", orderRequest.CurrencyCode);
            postDataAndValue.Add("first_name", orderRequest.CustomerFirstName);
            postDataAndValue.Add("last_name", orderRequest.CustomerSecondName);
            postDataAndValue.Add("address1", orderRequest.DeliveryAddressAddressLine);
            postDataAndValue.Add("city", orderRequest.DeliveryAddressCity);
            postDataAndValue.Add("state", orderRequest.DeliveryAddressState);
            postDataAndValue.Add("country", orderRequest.DeliveryAddressCountry);
            postDataAndValue.Add("zip", orderRequest.DeliveryAddressZipCode);
            // This is the URL where the user will be redirected after the payment
            // is successfully performed. If this parameter is not passed, the buyer
            // remains on the PayPal site.
            postDataAndValue.Add("return", _httpContextAccessor?.HttpContext?.Resolve("/Payment/PaymentComplete"));
            // This is the URL where the user will be redirected when
            // he cancels the payment. 
            // If the parameter is not passed, the buyer remains on the PayPal site.
            postDataAndValue.Add("cancel_return", _httpContextAccessor?.HttpContext?.Resolve("/Payment/PaymentCancel"));
            // This is the URL where PayPal will pass information about the
            // transaction (IPN). If the parameter is not passed, the value from
            // the account settings will be used. If this value is not defined in
            // the account settings, IPN will not be used.
            postDataAndValue.Add("notify_url", _httpContextAccessor?.HttpContext?.Resolve("/Payment/PaymentCallBack"));

            int itemIndex = 1;

            foreach (OrderItemPaymentRequest item in orderRequest.Items)
            {
                postDataAndValue.Add("item_name_" + itemIndex.ToString(), item.ProductName);
                postDataAndValue.Add("amount_" + itemIndex.ToString(), item.Price.ToString());
                postDataAndValue.Add("item_number_" + itemIndex.ToString(), item.Id.ToString());
                postDataAndValue.Add("quantity_" + itemIndex.ToString(), item.Qty.ToString());

                itemIndex++;
            }

            postDataAndValue.Add("handling_cart", orderRequest.ShippingCharge.ToString());
            postDataAndValue.Add("address_override", "1");

            return paymentPostData;
        }

        public async Task<TransactionResult> HandleCallBackAsync(OrderPaymentRequest orderRequest, IFormCollection collection)
        {
            var transactionResult = new TransactionResult();
            var response = await ValidatePaymentNotificationAsync(collection);

            if (response == "VERIFIED")
            {
                var sAmountPaid = collection["mc_gross"];
                var transactionId = collection["txn_id"];

                decimal.TryParse(sAmountPaid, out var amountPaid);

                if (orderRequest.Total == amountPaid)
                {
                    transactionResult.PaymentToken = transactionId;
                    transactionResult.Amount = amountPaid;
                    transactionResult.PaymentMerchant = "PayPal";
                    transactionResult.PaymentOk = true;
                }
                else
                {
                    transactionResult.PaymentToken = transactionId;
                    transactionResult.Amount = amountPaid;
                    transactionResult.PaymentMerchant = "PayPal";
                    transactionResult.PaymentOk = false;
                }
            }

            return transactionResult;
        }

        private async Task<string> ValidatePaymentNotificationAsync(IFormCollection formCollection)
        {
            var paypalUrl = _configuration["PayPalPaymentPostToUrl"];            
            var postFormData = new StringBuilder();

            foreach (string key in formCollection.Keys)
            {
                postFormData.AppendFormat("&{0}={1}", Uri.EscapeDataString(key), Uri.EscapeDataString(formCollection[key]));
            }

            if (!formCollection.ContainsKey("cmd"))
            {
                postFormData.AppendFormat("&{0}={1}", "cmd", "_notify-validate");
            }

            var strRequest = postFormData.ToString();            
            var content = new StringContent(strRequest, Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await _httpClient.PostAsync(paypalUrl, content);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            return string.Empty;
        }

        public int GetOrderIdFor(IFormCollection collection)
        {
            return int.Parse(collection["custom"]);
        }
    }
}