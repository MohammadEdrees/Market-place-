import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_ar.dart';
import 'app_localizations_en.dart';

// ignore_for_file: type=lint

/// Callers can lookup localized strings with an instance of AppLocalizations
/// returned by `AppLocalizations.of(context)`.
///
/// Applications need to include `AppLocalizations.delegate()` in their app's
/// `localizationDelegates` list, and the locales they support in the app's
/// `supportedLocales` list. For example:
///
/// ```dart
/// import 'generated/app_localizations.dart';
///
/// return MaterialApp(
///   localizationsDelegates: AppLocalizations.localizationsDelegates,
///   supportedLocales: AppLocalizations.supportedLocales,
///   home: MyApplicationHome(),
/// );
/// ```
///
/// ## Update pubspec.yaml
///
/// Please make sure to update your pubspec.yaml to include the following
/// packages:
///
/// ```yaml
/// dependencies:
///   # Internationalization support.
///   flutter_localizations:
///     sdk: flutter
///   intl: any # Use the pinned version from flutter_localizations
///
///   # Rest of dependencies
/// ```
///
/// ## iOS Applications
///
/// iOS applications define key application metadata, including supported
/// locales, in an Info.plist file that is built into the application bundle.
/// To configure the locales supported by your app, you’ll need to edit this
/// file.
///
/// First, open your project’s ios/Runner.xcworkspace Xcode workspace file.
/// Then, in the Project Navigator, open the Info.plist file under the Runner
/// project’s Runner folder.
///
/// Next, select the Information Property List item, select Add Item from the
/// Editor menu, then select Localizations from the pop-up menu.
///
/// Select and expand the newly-created Localizations item then, for each
/// locale your application supports, add a new item and select the locale
/// you wish to add from the pop-up menu in the Value field. This list should
/// be consistent with the languages listed in the AppLocalizations.supportedLocales
/// property.
abstract class AppLocalizations {
  AppLocalizations(String locale)
    : localeName = intl.Intl.canonicalizedLocale(locale.toString());

  final String localeName;

  static AppLocalizations? of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations);
  }

  static const LocalizationsDelegate<AppLocalizations> delegate =
      _AppLocalizationsDelegate();

  /// A list of this localizations delegate along with the default localizations
  /// delegates.
  ///
  /// Returns a list of localizations delegates containing this delegate along with
  /// GlobalMaterialLocalizations.delegate, GlobalCupertinoLocalizations.delegate,
  /// and GlobalWidgetsLocalizations.delegate.
  ///
  /// Additional delegates can be added by appending to this list in
  /// MaterialApp. This list does not have to be used at all if a custom list
  /// of delegates is preferred or required.
  static const List<LocalizationsDelegate<dynamic>> localizationsDelegates =
      <LocalizationsDelegate<dynamic>>[
        delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
      ];

  /// A list of this localizations delegate's supported locales.
  static const List<Locale> supportedLocales = <Locale>[
    Locale('ar'),
    Locale('en'),
  ];

  /// No description provided for @commonAll.
  ///
  /// In en, this message translates to:
  /// **'All'**
  String get commonAll;

  /// No description provided for @commonCancel.
  ///
  /// In en, this message translates to:
  /// **'Cancel'**
  String get commonCancel;

  /// No description provided for @commonDelete.
  ///
  /// In en, this message translates to:
  /// **'Delete'**
  String get commonDelete;

  /// No description provided for @commonEdit.
  ///
  /// In en, this message translates to:
  /// **'Edit'**
  String get commonEdit;

  /// No description provided for @commonEmailInvalid.
  ///
  /// In en, this message translates to:
  /// **'Enter a valid email address.'**
  String get commonEmailInvalid;

  /// No description provided for @commonEmailLabel.
  ///
  /// In en, this message translates to:
  /// **'Email'**
  String get commonEmailLabel;

  /// No description provided for @commonEmailRequired.
  ///
  /// In en, this message translates to:
  /// **'Email is required.'**
  String get commonEmailRequired;

  /// No description provided for @commonGenericError.
  ///
  /// In en, this message translates to:
  /// **'Something went wrong. Please try again.'**
  String get commonGenericError;

  /// No description provided for @commonLanguage.
  ///
  /// In en, this message translates to:
  /// **'Language'**
  String get commonLanguage;

  /// No description provided for @commonLanguageArabic.
  ///
  /// In en, this message translates to:
  /// **'العربية'**
  String get commonLanguageArabic;

  /// No description provided for @commonLanguageEnglish.
  ///
  /// In en, this message translates to:
  /// **'English'**
  String get commonLanguageEnglish;

  /// No description provided for @commonMaxCharacters.
  ///
  /// In en, this message translates to:
  /// **'Max {count} characters.'**
  String commonMaxCharacters(String count);

  /// No description provided for @commonMinCharacters.
  ///
  /// In en, this message translates to:
  /// **'At least 6 characters.'**
  String get commonMinCharacters;

  /// No description provided for @commonNameRequired.
  ///
  /// In en, this message translates to:
  /// **'Name is required.'**
  String get commonNameRequired;

  /// No description provided for @commonPasswordLabel.
  ///
  /// In en, this message translates to:
  /// **'Password'**
  String get commonPasswordLabel;

  /// No description provided for @commonPasswordRequired.
  ///
  /// In en, this message translates to:
  /// **'Password is required.'**
  String get commonPasswordRequired;

  /// No description provided for @commonPasswordsDoNotMatch.
  ///
  /// In en, this message translates to:
  /// **'Passwords do not match.'**
  String get commonPasswordsDoNotMatch;

  /// No description provided for @commonRequired.
  ///
  /// In en, this message translates to:
  /// **'Required.'**
  String get commonRequired;

  /// No description provided for @commonSaveChanges.
  ///
  /// In en, this message translates to:
  /// **'Save changes'**
  String get commonSaveChanges;

  /// No description provided for @commonSomethingWentWrong.
  ///
  /// In en, this message translates to:
  /// **'Something went wrong.'**
  String get commonSomethingWentWrong;

  /// No description provided for @commonTryAgain.
  ///
  /// In en, this message translates to:
  /// **'Try again'**
  String get commonTryAgain;

  /// No description provided for @navBrowse.
  ///
  /// In en, this message translates to:
  /// **'Browse'**
  String get navBrowse;

  /// No description provided for @navListings.
  ///
  /// In en, this message translates to:
  /// **'Listings'**
  String get navListings;

  /// No description provided for @navOrders.
  ///
  /// In en, this message translates to:
  /// **'Orders'**
  String get navOrders;

  /// No description provided for @navProfile.
  ///
  /// In en, this message translates to:
  /// **'Profile'**
  String get navProfile;

  /// No description provided for @navServices.
  ///
  /// In en, this message translates to:
  /// **'Services'**
  String get navServices;

  /// No description provided for @loginCreateAccount.
  ///
  /// In en, this message translates to:
  /// **'New here? Create an account'**
  String get loginCreateAccount;

  /// No description provided for @loginSignIn.
  ///
  /// In en, this message translates to:
  /// **'Sign in'**
  String get loginSignIn;

  /// No description provided for @loginTagline.
  ///
  /// In en, this message translates to:
  /// **'Buy products, reserve services, or sell your own.'**
  String get loginTagline;

  /// No description provided for @registerClientHint.
  ///
  /// In en, this message translates to:
  /// **'Browse the catalogue, buy products and reserve services.'**
  String get registerClientHint;

  /// No description provided for @registerConfirmPassword.
  ///
  /// In en, this message translates to:
  /// **'Confirm password'**
  String get registerConfirmPassword;

  /// No description provided for @registerCreateAccount.
  ///
  /// In en, this message translates to:
  /// **'Create account'**
  String get registerCreateAccount;

  /// No description provided for @registerFullName.
  ///
  /// In en, this message translates to:
  /// **'Full name'**
  String get registerFullName;

  /// No description provided for @registerHaveAccount.
  ///
  /// In en, this message translates to:
  /// **'I already have an account'**
  String get registerHaveAccount;

  /// No description provided for @registerHeading.
  ///
  /// In en, this message translates to:
  /// **'Join Market Workplace'**
  String get registerHeading;

  /// No description provided for @registerLocation.
  ///
  /// In en, this message translates to:
  /// **'Location'**
  String get registerLocation;

  /// No description provided for @registerOptionalContact.
  ///
  /// In en, this message translates to:
  /// **'Optional contact details'**
  String get registerOptionalContact;

  /// No description provided for @registerPhone.
  ///
  /// In en, this message translates to:
  /// **'Phone'**
  String get registerPhone;

  /// No description provided for @registerProviderHint.
  ///
  /// In en, this message translates to:
  /// **'Sell products and offer services of your own.'**
  String get registerProviderHint;

  /// No description provided for @registerSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Choose how you want to use the marketplace.'**
  String get registerSubtitle;

  /// No description provided for @registerTitle.
  ///
  /// In en, this message translates to:
  /// **'Create account'**
  String get registerTitle;

  /// No description provided for @productsEmpty.
  ///
  /// In en, this message translates to:
  /// **'No products match your search.'**
  String get productsEmpty;

  /// No description provided for @productsLoadError.
  ///
  /// In en, this message translates to:
  /// **'Could not load products.'**
  String get productsLoadError;

  /// No description provided for @productsSearchHint.
  ///
  /// In en, this message translates to:
  /// **'Search products'**
  String get productsSearchHint;

  /// No description provided for @productsTitle.
  ///
  /// In en, this message translates to:
  /// **'Browse'**
  String get productsTitle;

  /// No description provided for @servicesEmpty.
  ///
  /// In en, this message translates to:
  /// **'No services match your search.'**
  String get servicesEmpty;

  /// No description provided for @servicesLoadError.
  ///
  /// In en, this message translates to:
  /// **'Could not load services.'**
  String get servicesLoadError;

  /// No description provided for @servicesSearchHint.
  ///
  /// In en, this message translates to:
  /// **'Search services'**
  String get servicesSearchHint;

  /// No description provided for @servicesTitle.
  ///
  /// In en, this message translates to:
  /// **'Services'**
  String get servicesTitle;

  /// No description provided for @productDetailBuy.
  ///
  /// In en, this message translates to:
  /// **'Buy'**
  String get productDetailBuy;

  /// No description provided for @productDetailBuyNow.
  ///
  /// In en, this message translates to:
  /// **'Buy now'**
  String get productDetailBuyNow;

  /// No description provided for @productDetailConfirmMessage.
  ///
  /// In en, this message translates to:
  /// **'Buy \"{name}\" for {price}?'**
  String productDetailConfirmMessage(String name, String price);

  /// No description provided for @productDetailConfirmTitle.
  ///
  /// In en, this message translates to:
  /// **'Confirm purchase'**
  String get productDetailConfirmTitle;

  /// No description provided for @productDetailEditListing.
  ///
  /// In en, this message translates to:
  /// **'Edit listing'**
  String get productDetailEditListing;

  /// No description provided for @productDetailInStock.
  ///
  /// In en, this message translates to:
  /// **'{count} in stock'**
  String productDetailInStock(String count);

  /// No description provided for @productDetailNoneLeft.
  ///
  /// In en, this message translates to:
  /// **'None left'**
  String get productDetailNoneLeft;

  /// No description provided for @productDetailOrderPlaced.
  ///
  /// In en, this message translates to:
  /// **'Order placed — track it under Orders.'**
  String get productDetailOrderPlaced;

  /// No description provided for @productDetailSold.
  ///
  /// In en, this message translates to:
  /// **'{count} sold'**
  String productDetailSold(String count);

  /// No description provided for @productDetailTitle.
  ///
  /// In en, this message translates to:
  /// **'Product'**
  String get productDetailTitle;

  /// No description provided for @productDetailYours.
  ///
  /// In en, this message translates to:
  /// **'This is your listing.'**
  String get productDetailYours;

  /// No description provided for @serviceDetailAbout.
  ///
  /// In en, this message translates to:
  /// **'About this service'**
  String get serviceDetailAbout;

  /// No description provided for @serviceDetailConfirmMessage.
  ///
  /// In en, this message translates to:
  /// **'Reserve \"{title}\" for {price}?'**
  String serviceDetailConfirmMessage(String title, String price);

  /// No description provided for @serviceDetailConfirmTitle.
  ///
  /// In en, this message translates to:
  /// **'Confirm reservation'**
  String get serviceDetailConfirmTitle;

  /// No description provided for @serviceDetailContact.
  ///
  /// In en, this message translates to:
  /// **'Contact'**
  String get serviceDetailContact;

  /// No description provided for @serviceDetailEditService.
  ///
  /// In en, this message translates to:
  /// **'Edit service'**
  String get serviceDetailEditService;

  /// No description provided for @serviceDetailReserve.
  ///
  /// In en, this message translates to:
  /// **'Reserve'**
  String get serviceDetailReserve;

  /// No description provided for @serviceDetailReserved.
  ///
  /// In en, this message translates to:
  /// **'Reserved — track it under Orders.'**
  String get serviceDetailReserved;

  /// No description provided for @serviceDetailTitle.
  ///
  /// In en, this message translates to:
  /// **'Service'**
  String get serviceDetailTitle;

  /// No description provided for @serviceDetailUnavailable.
  ///
  /// In en, this message translates to:
  /// **'Currently unavailable'**
  String get serviceDetailUnavailable;

  /// No description provided for @serviceDetailYours.
  ///
  /// In en, this message translates to:
  /// **'This is your service.'**
  String get serviceDetailYours;

  /// No description provided for @ordersAnyStatus.
  ///
  /// In en, this message translates to:
  /// **'Any status'**
  String get ordersAnyStatus;

  /// No description provided for @ordersCategory.
  ///
  /// In en, this message translates to:
  /// **'Category'**
  String get ordersCategory;

  /// No description provided for @ordersCustomer.
  ///
  /// In en, this message translates to:
  /// **'Customer'**
  String get ordersCustomer;

  /// No description provided for @ordersDate.
  ///
  /// In en, this message translates to:
  /// **'Date'**
  String get ordersDate;

  /// No description provided for @ordersEmpty.
  ///
  /// In en, this message translates to:
  /// **'No orders yet.'**
  String get ordersEmpty;

  /// No description provided for @ordersFilterByStatus.
  ///
  /// In en, this message translates to:
  /// **'Filter by status'**
  String get ordersFilterByStatus;

  /// No description provided for @ordersNumber.
  ///
  /// In en, this message translates to:
  /// **'Order #{id}'**
  String ordersNumber(String id);

  /// No description provided for @ordersProductPurchase.
  ///
  /// In en, this message translates to:
  /// **'Product purchase'**
  String get ordersProductPurchase;

  /// No description provided for @ordersProductsFilter.
  ///
  /// In en, this message translates to:
  /// **'Products'**
  String get ordersProductsFilter;

  /// No description provided for @ordersSearchHint.
  ///
  /// In en, this message translates to:
  /// **'Search orders'**
  String get ordersSearchHint;

  /// No description provided for @ordersServiceReservation.
  ///
  /// In en, this message translates to:
  /// **'Service reservation'**
  String get ordersServiceReservation;

  /// No description provided for @ordersServicesFilter.
  ///
  /// In en, this message translates to:
  /// **'Services'**
  String get ordersServicesFilter;

  /// No description provided for @ordersStatus.
  ///
  /// In en, this message translates to:
  /// **'Status'**
  String get ordersStatus;

  /// No description provided for @ordersStatusUpdated.
  ///
  /// In en, this message translates to:
  /// **'Order #{id} → {status}'**
  String ordersStatusUpdated(String id, String status);

  /// No description provided for @ordersTitle.
  ///
  /// In en, this message translates to:
  /// **'Orders'**
  String get ordersTitle;

  /// No description provided for @ordersTotal.
  ///
  /// In en, this message translates to:
  /// **'Total'**
  String get ordersTotal;

  /// No description provided for @ordersUpdateStatus.
  ///
  /// In en, this message translates to:
  /// **'Update status'**
  String get ordersUpdateStatus;

  /// No description provided for @myListingsCreate.
  ///
  /// In en, this message translates to:
  /// **'Create'**
  String get myListingsCreate;

  /// No description provided for @myListingsDeleteProductMessage.
  ///
  /// In en, this message translates to:
  /// **'\"{name}\" will be removed permanently.'**
  String myListingsDeleteProductMessage(String name);

  /// No description provided for @myListingsDeleteProductTitle.
  ///
  /// In en, this message translates to:
  /// **'Delete product?'**
  String get myListingsDeleteProductTitle;

  /// No description provided for @myListingsDeleteServiceMessage.
  ///
  /// In en, this message translates to:
  /// **'\"{title}\" will be removed permanently.'**
  String myListingsDeleteServiceMessage(String title);

  /// No description provided for @myListingsDeleteServiceTitle.
  ///
  /// In en, this message translates to:
  /// **'Delete service?'**
  String get myListingsDeleteServiceTitle;

  /// No description provided for @myListingsDeletedProduct.
  ///
  /// In en, this message translates to:
  /// **'Deleted \"{name}\".'**
  String myListingsDeletedProduct(String name);

  /// No description provided for @myListingsDeletedService.
  ///
  /// In en, this message translates to:
  /// **'Deleted \"{title}\".'**
  String myListingsDeletedService(String title);

  /// No description provided for @myListingsEmptyProducts.
  ///
  /// In en, this message translates to:
  /// **'You have no products yet. Create your first one.'**
  String get myListingsEmptyProducts;

  /// No description provided for @myListingsEmptyServices.
  ///
  /// In en, this message translates to:
  /// **'You have no services yet. Create your first one.'**
  String get myListingsEmptyServices;

  /// No description provided for @myListingsNewProduct.
  ///
  /// In en, this message translates to:
  /// **'New product'**
  String get myListingsNewProduct;

  /// No description provided for @myListingsNewService.
  ///
  /// In en, this message translates to:
  /// **'New service'**
  String get myListingsNewService;

  /// No description provided for @myListingsProductsTab.
  ///
  /// In en, this message translates to:
  /// **'Products'**
  String get myListingsProductsTab;

  /// No description provided for @myListingsServicesTab.
  ///
  /// In en, this message translates to:
  /// **'Services'**
  String get myListingsServicesTab;

  /// No description provided for @myListingsStockAndSold.
  ///
  /// In en, this message translates to:
  /// **'{stock} in stock · {sold} sold'**
  String myListingsStockAndSold(String sold, String stock);

  /// No description provided for @myListingsTitle.
  ///
  /// In en, this message translates to:
  /// **'My listings'**
  String get myListingsTitle;

  /// No description provided for @listingFormCategory.
  ///
  /// In en, this message translates to:
  /// **'Category'**
  String get listingFormCategory;

  /// No description provided for @listingFormCategoryHintProduct.
  ///
  /// In en, this message translates to:
  /// **'e.g. Electronics'**
  String get listingFormCategoryHintProduct;

  /// No description provided for @listingFormCategoryHintService.
  ///
  /// In en, this message translates to:
  /// **'e.g. Repairs'**
  String get listingFormCategoryHintService;

  /// No description provided for @listingFormChangesSaved.
  ///
  /// In en, this message translates to:
  /// **'Changes saved.'**
  String get listingFormChangesSaved;

  /// No description provided for @listingFormContactHint.
  ///
  /// In en, this message translates to:
  /// **'Phone, email or WhatsApp'**
  String get listingFormContactHint;

  /// No description provided for @listingFormContactInfo.
  ///
  /// In en, this message translates to:
  /// **'Contact info'**
  String get listingFormContactInfo;

  /// No description provided for @listingFormCost.
  ///
  /// In en, this message translates to:
  /// **'Cost (USD)'**
  String get listingFormCost;

  /// No description provided for @listingFormCreateProduct.
  ///
  /// In en, this message translates to:
  /// **'Create product'**
  String get listingFormCreateProduct;

  /// No description provided for @listingFormCreateService.
  ///
  /// In en, this message translates to:
  /// **'Create service'**
  String get listingFormCreateService;

  /// No description provided for @listingFormDescription.
  ///
  /// In en, this message translates to:
  /// **'Description'**
  String get listingFormDescription;

  /// No description provided for @listingFormDescriptionHint.
  ///
  /// In en, this message translates to:
  /// **'What do you offer?'**
  String get listingFormDescriptionHint;

  /// No description provided for @listingFormEditProduct.
  ///
  /// In en, this message translates to:
  /// **'Edit product'**
  String get listingFormEditProduct;

  /// No description provided for @listingFormEditService.
  ///
  /// In en, this message translates to:
  /// **'Edit service'**
  String get listingFormEditService;

  /// No description provided for @listingFormLocation.
  ///
  /// In en, this message translates to:
  /// **'Location / service area'**
  String get listingFormLocation;

  /// No description provided for @listingFormName.
  ///
  /// In en, this message translates to:
  /// **'Name'**
  String get listingFormName;

  /// No description provided for @listingFormNewProduct.
  ///
  /// In en, this message translates to:
  /// **'New product'**
  String get listingFormNewProduct;

  /// No description provided for @listingFormNewService.
  ///
  /// In en, this message translates to:
  /// **'New service'**
  String get listingFormNewService;

  /// No description provided for @listingFormNonNegative.
  ///
  /// In en, this message translates to:
  /// **'Enter 0 or more.'**
  String get listingFormNonNegative;

  /// No description provided for @listingFormOffers.
  ///
  /// In en, this message translates to:
  /// **'Current offers (optional)'**
  String get listingFormOffers;

  /// No description provided for @listingFormOffersHint.
  ///
  /// In en, this message translates to:
  /// **'e.g. 20% off the first booking'**
  String get listingFormOffersHint;

  /// No description provided for @listingFormPhotoAdded.
  ///
  /// In en, this message translates to:
  /// **'Photo added.'**
  String get listingFormPhotoAdded;

  /// No description provided for @listingFormPhotoRemoved.
  ///
  /// In en, this message translates to:
  /// **'Photo removed.'**
  String get listingFormPhotoRemoved;

  /// No description provided for @listingFormPhotos.
  ///
  /// In en, this message translates to:
  /// **'Photos'**
  String get listingFormPhotos;

  /// No description provided for @listingFormPrice.
  ///
  /// In en, this message translates to:
  /// **'Price (USD)'**
  String get listingFormPrice;

  /// No description provided for @listingFormSaveFirstProduct.
  ///
  /// In en, this message translates to:
  /// **'Save the product first, then add photos here.'**
  String get listingFormSaveFirstProduct;

  /// No description provided for @listingFormSaveFirstService.
  ///
  /// In en, this message translates to:
  /// **'Save the service first, then add photos here.'**
  String get listingFormSaveFirstService;

  /// No description provided for @listingFormSavedAddPhotos.
  ///
  /// In en, this message translates to:
  /// **'Saved — now add some photos.'**
  String get listingFormSavedAddPhotos;

  /// No description provided for @listingFormSku.
  ///
  /// In en, this message translates to:
  /// **'SKU'**
  String get listingFormSku;

  /// No description provided for @listingFormStock.
  ///
  /// In en, this message translates to:
  /// **'Stock'**
  String get listingFormStock;

  /// No description provided for @listingFormThumbnailHint.
  ///
  /// In en, this message translates to:
  /// **'First photo is shown as the thumbnail.'**
  String get listingFormThumbnailHint;

  /// No description provided for @listingFormTitle.
  ///
  /// In en, this message translates to:
  /// **'Title'**
  String get listingFormTitle;

  /// No description provided for @listingFormValidAmount.
  ///
  /// In en, this message translates to:
  /// **'Enter a valid amount.'**
  String get listingFormValidAmount;

  /// No description provided for @profileBio.
  ///
  /// In en, this message translates to:
  /// **'Bio'**
  String get profileBio;

  /// No description provided for @profileChoosePhoto.
  ///
  /// In en, this message translates to:
  /// **'Choose photo'**
  String get profileChoosePhoto;

  /// No description provided for @profileClearToRemove.
  ///
  /// In en, this message translates to:
  /// **'Leave empty to remove.'**
  String get profileClearToRemove;

  /// No description provided for @profileEditProfile.
  ///
  /// In en, this message translates to:
  /// **'Edit profile'**
  String get profileEditProfile;

  /// No description provided for @profileFullName.
  ///
  /// In en, this message translates to:
  /// **'Full name'**
  String get profileFullName;

  /// No description provided for @profileLocation.
  ///
  /// In en, this message translates to:
  /// **'Location'**
  String get profileLocation;

  /// No description provided for @profilePhone.
  ///
  /// In en, this message translates to:
  /// **'Phone'**
  String get profilePhone;

  /// No description provided for @profilePhotoRemoved.
  ///
  /// In en, this message translates to:
  /// **'Photo removed.'**
  String get profilePhotoRemoved;

  /// No description provided for @profilePhotoUpdated.
  ///
  /// In en, this message translates to:
  /// **'Photo updated.'**
  String get profilePhotoUpdated;

  /// No description provided for @profileRemovePhoto.
  ///
  /// In en, this message translates to:
  /// **'Remove photo'**
  String get profileRemovePhoto;

  /// No description provided for @profileSignOut.
  ///
  /// In en, this message translates to:
  /// **'Sign out'**
  String get profileSignOut;

  /// No description provided for @profileSignOutMessage.
  ///
  /// In en, this message translates to:
  /// **'You will need to sign in again to continue.'**
  String get profileSignOutMessage;

  /// No description provided for @profileSignOutTitle.
  ///
  /// In en, this message translates to:
  /// **'Sign out?'**
  String get profileSignOutTitle;

  /// No description provided for @profileTitle.
  ///
  /// In en, this message translates to:
  /// **'Profile'**
  String get profileTitle;

  /// No description provided for @profileUpdated.
  ///
  /// In en, this message translates to:
  /// **'Profile updated.'**
  String get profileUpdated;

  /// No description provided for @statusActive.
  ///
  /// In en, this message translates to:
  /// **'Active'**
  String get statusActive;

  /// No description provided for @statusCancelled.
  ///
  /// In en, this message translates to:
  /// **'Cancelled'**
  String get statusCancelled;

  /// No description provided for @statusClient.
  ///
  /// In en, this message translates to:
  /// **'Client'**
  String get statusClient;

  /// No description provided for @statusCompleted.
  ///
  /// In en, this message translates to:
  /// **'Completed'**
  String get statusCompleted;

  /// No description provided for @statusConfirmed.
  ///
  /// In en, this message translates to:
  /// **'Confirmed'**
  String get statusConfirmed;

  /// No description provided for @statusDashboard.
  ///
  /// In en, this message translates to:
  /// **'Dashboard'**
  String get statusDashboard;

  /// No description provided for @statusInactive.
  ///
  /// In en, this message translates to:
  /// **'Inactive'**
  String get statusInactive;

  /// No description provided for @statusLowStock.
  ///
  /// In en, this message translates to:
  /// **'Low stock'**
  String get statusLowStock;

  /// No description provided for @statusMobile.
  ///
  /// In en, this message translates to:
  /// **'Mobile'**
  String get statusMobile;

  /// No description provided for @statusOutOfStock.
  ///
  /// In en, this message translates to:
  /// **'Out of stock'**
  String get statusOutOfStock;

  /// No description provided for @statusProcessing.
  ///
  /// In en, this message translates to:
  /// **'Processing'**
  String get statusProcessing;

  /// No description provided for @statusProvider.
  ///
  /// In en, this message translates to:
  /// **'Provider'**
  String get statusProvider;

  /// No description provided for @statusRefunded.
  ///
  /// In en, this message translates to:
  /// **'Refunded'**
  String get statusRefunded;

  /// No description provided for @statusReserved.
  ///
  /// In en, this message translates to:
  /// **'Reserved'**
  String get statusReserved;

  /// No description provided for @errorsAccountExists.
  ///
  /// In en, this message translates to:
  /// **'An account with this email already exists.'**
  String get errorsAccountExists;

  /// No description provided for @errorsCheckCredentials.
  ///
  /// In en, this message translates to:
  /// **'Check the credentials and try again.'**
  String get errorsCheckCredentials;

  /// No description provided for @errorsDashboardAccount.
  ///
  /// In en, this message translates to:
  /// **'This account belongs to the web dashboard. Sign in with a Client or Provider account.'**
  String get errorsDashboardAccount;

  /// No description provided for @errorsEmailAlreadyRegistered.
  ///
  /// In en, this message translates to:
  /// **'Sign in instead or choose another email address.'**
  String get errorsEmailAlreadyRegistered;

  /// No description provided for @errorsEmailFieldRequired.
  ///
  /// In en, this message translates to:
  /// **'The Email field is required.'**
  String get errorsEmailFieldRequired;

  /// No description provided for @errorsEmailNotValid.
  ///
  /// In en, this message translates to:
  /// **'The Email field is not a valid e-mail address.'**
  String get errorsEmailNotValid;

  /// No description provided for @errorsInvalidCredentials.
  ///
  /// In en, this message translates to:
  /// **'Invalid email or password.'**
  String get errorsInvalidCredentials;

  /// No description provided for @errorsInvalidRoleClientProvider.
  ///
  /// In en, this message translates to:
  /// **'Role must be Client or Provider.'**
  String get errorsInvalidRoleClientProvider;

  /// No description provided for @errorsPasswordFieldRequired.
  ///
  /// In en, this message translates to:
  /// **'The Password field is required.'**
  String get errorsPasswordFieldRequired;

  /// No description provided for @errorsPasswordTooShort.
  ///
  /// In en, this message translates to:
  /// **'Password must be at least 6 characters.'**
  String get errorsPasswordTooShort;

  /// No description provided for @networkCancelled.
  ///
  /// In en, this message translates to:
  /// **'Request cancelled.'**
  String get networkCancelled;

  /// No description provided for @networkConflict.
  ///
  /// In en, this message translates to:
  /// **'Conflict'**
  String get networkConflict;

  /// No description provided for @networkConnectionFailed.
  ///
  /// In en, this message translates to:
  /// **'Connection failed'**
  String get networkConnectionFailed;

  /// No description provided for @networkFileTooLarge.
  ///
  /// In en, this message translates to:
  /// **'File too large'**
  String get networkFileTooLarge;

  /// No description provided for @networkInvalidRequest.
  ///
  /// In en, this message translates to:
  /// **'Invalid request'**
  String get networkInvalidRequest;

  /// No description provided for @networkNotAllowed.
  ///
  /// In en, this message translates to:
  /// **'Not allowed'**
  String get networkNotAllowed;

  /// No description provided for @networkNotFound.
  ///
  /// In en, this message translates to:
  /// **'Not found'**
  String get networkNotFound;

  /// No description provided for @networkRequestFailed.
  ///
  /// In en, this message translates to:
  /// **'Request failed'**
  String get networkRequestFailed;

  /// No description provided for @networkServerError.
  ///
  /// In en, this message translates to:
  /// **'Server error'**
  String get networkServerError;

  /// No description provided for @networkSignInRequired.
  ///
  /// In en, this message translates to:
  /// **'Sign-in required'**
  String get networkSignInRequired;

  /// No description provided for @networkTimeout.
  ///
  /// In en, this message translates to:
  /// **'The server took too long to respond.'**
  String get networkTimeout;

  /// No description provided for @networkUnexpected.
  ///
  /// In en, this message translates to:
  /// **'Unexpected network error.'**
  String get networkUnexpected;

  /// No description provided for @networkUnreachable.
  ///
  /// In en, this message translates to:
  /// **'Could not reach the server. Check your connection.'**
  String get networkUnreachable;
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  Future<AppLocalizations> load(Locale locale) {
    return SynchronousFuture<AppLocalizations>(lookupAppLocalizations(locale));
  }

  @override
  bool isSupported(Locale locale) =>
      <String>['ar', 'en'].contains(locale.languageCode);

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

AppLocalizations lookupAppLocalizations(Locale locale) {
  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'ar':
      return AppLocalizationsAr();
    case 'en':
      return AppLocalizationsEn();
  }

  throw FlutterError(
    'AppLocalizations.delegate failed to load unsupported locale "$locale". This is likely '
    'an issue with the localizations generation tool. Please file an issue '
    'on GitHub with a reproducible sample app and the gen-l10n configuration '
    'that was used.',
  );
}
