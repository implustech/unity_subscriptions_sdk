<img src="https://apptilaus.com/files/logo_green.svg"  width="300">

## Apptilaus Subscriptions SDK for Unity

[![Tweet](https://img.shields.io/twitter/url/http/shields.io.svg?style=social)](https://twitter.com/intent/tweet?text=Analyse%20subscriptions%20for%20your%20app!%20No%20SDK%20required!%20&url=http://apptilaus.com&hashtags=subscriptions,apps,appstore,unity,analytics)&nbsp;[![Platform](http://img.shields.io/badge/platform-unity-blue.svg?style=flat)](http://unity.com)&nbsp;[![Language](http://img.shields.io/badge/language-csharp-brightgreen.svg?style=flat)](https://github.com/Apptilaus/unity_subscriptions_sdk/)&nbsp;[![License](https://img.shields.io/cocoapods/l/Apptilaus.svg?style=flat)](https://github.com/Apptilaus/unity_subscriptions_sdk/)&nbsp;

## Overview ##

**Apptilaus** Unity SDK is an open-source SDK that provides a simplest way to analyse cross-device subscriptions via [**Apptilaus Service**](https://apptilaus.com) for iOS App Store and Google Play Store.

## Table of contents

* [Working with the Library](#integration)
   * [Prerequisite](#prerequisite)   
   * [Add the SDK to your project](#sdk-add)
   * [Initial Setup](#basic-setup)
      * [Register Subscriptions](#register-subscription)
      * [Register Subscriptions with parameters](#register-subscription-params)
   * [Advanced Setup](#advanced-setup)
      * [Session Tracking](#session-tracking)
      * [GDPR Right to Erasure](#gdpr-opt-out)
      * [User Enrichment](#user-data)
      * [On-Premise Setup](#on-premise)
   * [Build your app](#build-the-app)
* [Licence](#licence)

---

## <a id="integration">Working with the Library

In order to use the Apptilaus SDK, you must obtain `AppID` and `AppToken` for your app. You can find the App ID and App Token in the [admin panel][admin-panel].

Here is the steps to integrate the Apptilaus Unity SDK into your project using Unity.

-----

### <a id="prerequisite"></a>Prerequisite 

Before adding ApptilausSDK to your project make sure you've successfully downloaded and integrated [**Unity IAP**][unityiap] package.

-----

### <a id="sdk-add"></a>Add the SDK to your project

Download the latest version from our [releases page][releases].

Open your project in the Unity Editor and navigate to `Assets → Import Package → Custom Package`. Select the downloaded `ApptilausSDK.unitypackage`.

---

### <a id="basic-setup"></a>Initial Setup

Add the prefab located at `Assets/ApptilausSDK/Apptilaus.prefab` to the first scene. 

Edit the parameters of the `Apptilaus` script in the Inspector menu of the added prefab.

You have the possibility to set up the following options on the Apptilaus prefab:

* [Setting App Credentials](#app-credentials)

<a id="app-credentials">Replace `{AppID}` and `{AppToken}` with your App ID and App Token from your [admin panel][admin-panel].

* [Enable Session Tracking](#session-tracking)

<a id="session-tracking">Depending on whether or not you build your app for extended analytics integrations, you might consider to enable sessions tracking.

---

#### <a id="register-subscription"></a>Register Subscriptions

To register subscriptions, call `ApptilausManager.Share.Purchase` inside the `PurchaseProcessingResult` method of your `IStoreListener` implementation. Pass `PurchaseEventArgs` object as a parameter.

```csharp

	public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e) {
		ApptilausManager.Shared.Purchase(e);
		return PurchaseProcessingResult.Complete;
	}

```

---

#### <a id="register-subscription-params"></a>Register Subscriptions with parameters

To register subscriptions with custom parameters you must pass another parameter to the Purchase method. It must be a `Dictionary<string,string>` which contains an arbitrary number of Key/Value pairs, representing your custom parameters.

```csharp

	public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e) {		
		Dictionary<string,string> parameters = new Dictionary<string,string>();
		parameters.Add("custom_parameter","custom_value");		
		ApptilausManager.Shared.Purchase(e, parameters);
		return PurchaseProcessingResult.Complete;
	}

```

-----

### <a id="advanced-setup"></a>Advanced Setup

#### <a id="session-tracking"></a>Session tracking

To register sessions you must perform the steps outlined above. See [Enable Session Tracking](#session-tracking).

---

#### <a id="gdpr-opt-out"></a>GDPR Right to Erasure

In accordance with article 17 of the EU's General Data Protection Regulation (GDPR), you can notify Apptilaus when a user has exercised their right to be forgotten. Calling the OptOut method will instruct the Apptilaus SDK to communicate the user's choice to be forgotten to the Apptilaus backend and data storage.

The method can take a delegate, which is called when the operation is completed, as an optional parameter.
```csharp

	public void OptOut(){
		ApptilausManager.Shared.OptOut(OnOptOutComplete);
	}
	
	private void OnOptOutComplete(bool success, string error){
		if(success){
			Debug.Log("Opt Out processed successfully");
		}
		else{
			Debug.LogError("Opt out error " + error);
		}
	}

```

Upon receiving this information, Apptilaus will erase the user's data and the Apptilaus SDK will stop tracking the user. No requests from this device will stored by Apptilaus in the future.

---

#### <a id="user-data"></a>User Enrichment

You can optionally set your internal user ID string to track user purchases. It could be done at any moment, e.g. during the app launch, or after app authentication process:

```csharp

	ApptilausManager.Shared.UserId = "CustomId";

```

---

#### <a id="on-premise"></a>On-premise Setup

To use custom on-premise solution you must set BaseURL property to the URL of your server.

```csharp

	ApptilausManager.Shared.BaseUrl = "https://your.custom.url.here";

```

---

### <a id="build-the-app"></a>Build your app

Build and run your app. If the build succeeds, you should carefully read the SDK logs in the console. After completing purchase, you should see the info log `[Apptilaus]: Purchase Processed`.

---

[apptilaus.com]:  http://apptilaus.com
[admin-panel]:   https://go.apptilaus.com

[releases]:    https://github.com/Apptilaus/unity_subscriptions_sdk/releases

[Examples]:  Examples/
[Example-iOS]:  Examples/Example-iOS
[Example-Android]:  Examples/Example-Android
[partmer-docs]:  Docs/English/
[partmer-docs-adjust]:  Docs/English/adjust.md
[partmer-docs-amplitude]:  Docs/English/amplitude.md
[partmer-docs-appmetrica]:  Docs/English/appmetrica.md
[partmer-docs-appsflyer]:  Docs/English/appsflyer.md

[unityiap]:  https://unity3d.com/learn/tutorials/topics/ads-analytics/integrating-unity-iap-your-game

## <a id="licence"></a>Licence and Copyright

The Apptilaus SDK is licensed under the MIT License.

**Apptilaus** (c) 2018-2019 All Rights Reserved

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

[![Analytics](https://ga-beacon.appspot.com/UA-125243602-3/unity_subscriptions_sdk/README.md)](https://github.com/igrigorik/ga-beacon)

