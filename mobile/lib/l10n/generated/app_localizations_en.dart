// ignore: unused_import
import 'package:intl/intl.dart' as intl;

import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppLocalizationsEn extends AppLocalizations {
  AppLocalizationsEn([String locale = 'en']) : super(locale);

  @override
  String get commonAll => 'All';

  @override
  String get commonCancel => 'Cancel';

  @override
  String get commonDelete => 'Delete';

  @override
  String get commonEdit => 'Edit';

  @override
  String get commonEmailInvalid => 'Enter a valid email address.';

  @override
  String get commonEmailLabel => 'Email';

  @override
  String get commonEmailRequired => 'Email is required.';

  @override
  String get commonGenericError => 'Something went wrong. Please try again.';

  @override
  String get commonLanguage => 'Language';

  @override
  String get commonLanguageArabic => 'العربية';

  @override
  String get commonLanguageEnglish => 'English';

  @override
  String commonMaxCharacters(String count) {
    return 'Max $count characters.';
  }

  @override
  String get commonMinCharacters => 'At least 6 characters.';

  @override
  String get commonNameRequired => 'Name is required.';

  @override
  String get commonPasswordLabel => 'Password';

  @override
  String get commonPasswordRequired => 'Password is required.';

  @override
  String get commonPasswordsDoNotMatch => 'Passwords do not match.';

  @override
  String get commonRequired => 'Required.';

  @override
  String get commonSaveChanges => 'Save changes';

  @override
  String get commonSomethingWentWrong => 'Something went wrong.';

  @override
  String get commonTryAgain => 'Try again';

  @override
  String get navBrowse => 'Browse';

  @override
  String get navListings => 'Listings';

  @override
  String get navOrders => 'Orders';

  @override
  String get navProfile => 'Profile';

  @override
  String get navServices => 'Services';

  @override
  String get loginCreateAccount => 'New here? Create an account';

  @override
  String get loginSignIn => 'Sign in';

  @override
  String get loginTagline =>
      'Buy products, reserve services, or sell your own.';

  @override
  String get registerClientHint =>
      'Browse the catalogue, buy products and reserve services.';

  @override
  String get registerConfirmPassword => 'Confirm password';

  @override
  String get registerCreateAccount => 'Create account';

  @override
  String get registerFullName => 'Full name';

  @override
  String get registerHaveAccount => 'I already have an account';

  @override
  String get registerHeading => 'Join Market Workplace';

  @override
  String get registerLocation => 'Location';

  @override
  String get registerOptionalContact => 'Optional contact details';

  @override
  String get registerPhone => 'Phone';

  @override
  String get registerProviderHint =>
      'Sell products and offer services of your own.';

  @override
  String get registerSubtitle => 'Choose how you want to use the marketplace.';

  @override
  String get registerTitle => 'Create account';

  @override
  String get productsEmpty => 'No products match your search.';

  @override
  String get productsLoadError => 'Could not load products.';

  @override
  String get productsSearchHint => 'Search products';

  @override
  String get productsTitle => 'Browse';

  @override
  String get servicesEmpty => 'No services match your search.';

  @override
  String get servicesLoadError => 'Could not load services.';

  @override
  String get servicesSearchHint => 'Search services';

  @override
  String get servicesTitle => 'Services';

  @override
  String get productDetailBuy => 'Buy';

  @override
  String get productDetailBuyNow => 'Buy now';

  @override
  String productDetailConfirmMessage(String name, String price) {
    return 'Buy \"$name\" for $price?';
  }

  @override
  String get productDetailConfirmTitle => 'Confirm purchase';

  @override
  String get productDetailEditListing => 'Edit listing';

  @override
  String productDetailInStock(String count) {
    return '$count in stock';
  }

  @override
  String get productDetailNoneLeft => 'None left';

  @override
  String get productDetailOrderPlaced =>
      'Order placed — track it under Orders.';

  @override
  String productDetailSold(String count) {
    return '$count sold';
  }

  @override
  String get productDetailTitle => 'Product';

  @override
  String get productDetailYours => 'This is your listing.';

  @override
  String get serviceDetailAbout => 'About this service';

  @override
  String serviceDetailConfirmMessage(String title, String price) {
    return 'Reserve \"$title\" for $price?';
  }

  @override
  String get serviceDetailConfirmTitle => 'Confirm reservation';

  @override
  String get serviceDetailContact => 'Contact';

  @override
  String get serviceDetailEditService => 'Edit service';

  @override
  String get serviceDetailReserve => 'Reserve';

  @override
  String get serviceDetailReserved => 'Reserved — track it under Orders.';

  @override
  String get serviceDetailTitle => 'Service';

  @override
  String get serviceDetailUnavailable => 'Currently unavailable';

  @override
  String get serviceDetailYours => 'This is your service.';

  @override
  String get ordersAnyStatus => 'Any status';

  @override
  String get ordersCategory => 'Category';

  @override
  String get ordersCustomer => 'Customer';

  @override
  String get ordersDate => 'Date';

  @override
  String get ordersEmpty => 'No orders yet.';

  @override
  String get ordersFilterByStatus => 'Filter by status';

  @override
  String ordersNumber(String id) {
    return 'Order #$id';
  }

  @override
  String get ordersProductPurchase => 'Product purchase';

  @override
  String get ordersProductsFilter => 'Products';

  @override
  String get ordersSearchHint => 'Search orders';

  @override
  String get ordersServiceReservation => 'Service reservation';

  @override
  String get ordersServicesFilter => 'Services';

  @override
  String get ordersStatus => 'Status';

  @override
  String ordersStatusUpdated(String id, String status) {
    return 'Order #$id → $status';
  }

  @override
  String get ordersTitle => 'Orders';

  @override
  String get ordersTotal => 'Total';

  @override
  String get ordersUpdateStatus => 'Update status';

  @override
  String get myListingsCreate => 'Create';

  @override
  String myListingsDeleteProductMessage(String name) {
    return '\"$name\" will be removed permanently.';
  }

  @override
  String get myListingsDeleteProductTitle => 'Delete product?';

  @override
  String myListingsDeleteServiceMessage(String title) {
    return '\"$title\" will be removed permanently.';
  }

  @override
  String get myListingsDeleteServiceTitle => 'Delete service?';

  @override
  String myListingsDeletedProduct(String name) {
    return 'Deleted \"$name\".';
  }

  @override
  String myListingsDeletedService(String title) {
    return 'Deleted \"$title\".';
  }

  @override
  String get myListingsEmptyProducts =>
      'You have no products yet. Create your first one.';

  @override
  String get myListingsEmptyServices =>
      'You have no services yet. Create your first one.';

  @override
  String get myListingsNewProduct => 'New product';

  @override
  String get myListingsNewService => 'New service';

  @override
  String get myListingsProductsTab => 'Products';

  @override
  String get myListingsServicesTab => 'Services';

  @override
  String myListingsStockAndSold(String sold, String stock) {
    return '$stock in stock · $sold sold';
  }

  @override
  String get myListingsTitle => 'My listings';

  @override
  String get listingFormCategory => 'Category';

  @override
  String get listingFormCategoryHintProduct => 'e.g. Electronics';

  @override
  String get listingFormCategoryHintService => 'e.g. Repairs';

  @override
  String get listingFormChangesSaved => 'Changes saved.';

  @override
  String get listingFormContactHint => 'Phone, email or WhatsApp';

  @override
  String get listingFormContactInfo => 'Contact info';

  @override
  String get listingFormCost => 'Cost (USD)';

  @override
  String get listingFormCreateProduct => 'Create product';

  @override
  String get listingFormCreateService => 'Create service';

  @override
  String get listingFormDescription => 'Description';

  @override
  String get listingFormDescriptionHint => 'What do you offer?';

  @override
  String get listingFormEditProduct => 'Edit product';

  @override
  String get listingFormEditService => 'Edit service';

  @override
  String get listingFormLocation => 'Location / service area';

  @override
  String get listingFormName => 'Name';

  @override
  String get listingFormNewProduct => 'New product';

  @override
  String get listingFormNewService => 'New service';

  @override
  String get listingFormNonNegative => 'Enter 0 or more.';

  @override
  String get listingFormOffers => 'Current offers (optional)';

  @override
  String get listingFormOffersHint => 'e.g. 20% off the first booking';

  @override
  String get listingFormPhotoAdded => 'Photo added.';

  @override
  String get listingFormPhotoRemoved => 'Photo removed.';

  @override
  String get listingFormPhotos => 'Photos';

  @override
  String get listingFormPrice => 'Price (USD)';

  @override
  String get listingFormSaveFirstProduct =>
      'Save the product first, then add photos here.';

  @override
  String get listingFormSaveFirstService =>
      'Save the service first, then add photos here.';

  @override
  String get listingFormSavedAddPhotos => 'Saved — now add some photos.';

  @override
  String get listingFormSku => 'SKU';

  @override
  String get listingFormStock => 'Stock';

  @override
  String get listingFormThumbnailHint =>
      'First photo is shown as the thumbnail.';

  @override
  String get listingFormTitle => 'Title';

  @override
  String get listingFormValidAmount => 'Enter a valid amount.';

  @override
  String get profileBio => 'Bio';

  @override
  String get profileChoosePhoto => 'Choose photo';

  @override
  String get profileClearToRemove => 'Leave empty to remove.';

  @override
  String get profileEditProfile => 'Edit profile';

  @override
  String get profileFullName => 'Full name';

  @override
  String get profileLocation => 'Location';

  @override
  String get profileNoPlan => 'No plan assigned yet.';

  @override
  String get profilePhone => 'Phone';

  @override
  String get profilePhotoRemoved => 'Photo removed.';

  @override
  String get profilePhotoUpdated => 'Photo updated.';

  @override
  String get profileRemovePhoto => 'Remove photo';

  @override
  String get profileSignOut => 'Sign out';

  @override
  String get profileSignOutMessage =>
      'You will need to sign in again to continue.';

  @override
  String get profileSignOutTitle => 'Sign out?';

  @override
  String get profileSubscription => 'Subscription';

  @override
  String get profileTitle => 'Profile';

  @override
  String get profileUpdated => 'Profile updated.';

  @override
  String get statusActive => 'Active';

  @override
  String get statusCancelled => 'Cancelled';

  @override
  String get statusClient => 'Client';

  @override
  String get statusCompleted => 'Completed';

  @override
  String get statusConfirmed => 'Confirmed';

  @override
  String get statusDashboard => 'Dashboard';

  @override
  String get statusExpired => 'Expired';

  @override
  String get statusInactive => 'Inactive';

  @override
  String get statusLowStock => 'Low stock';

  @override
  String get statusMobile => 'Mobile';

  @override
  String get statusOutOfStock => 'Out of stock';

  @override
  String get statusProcessing => 'Processing';

  @override
  String get statusProvider => 'Provider';

  @override
  String get statusRefunded => 'Refunded';

  @override
  String get statusReserved => 'Reserved';

  @override
  String get planBasic => 'Basic';

  @override
  String get planEnterprise => 'Enterprise';

  @override
  String get planPremium => 'Premium';

  @override
  String get subscriptionAutoRenewOff => 'Will not renew automatically';

  @override
  String get subscriptionAutoRenewOn => 'Renews automatically';

  @override
  String get subscriptionOpenEnded => 'Open-ended';

  @override
  String get subscriptionPerMonth => 'per month';

  @override
  String get subscriptionPerYear => 'per year';

  @override
  String get errorsAccountExists =>
      'An account with this email already exists.';

  @override
  String get errorsCheckCredentials => 'Check the credentials and try again.';

  @override
  String get errorsDashboardAccount =>
      'This account belongs to the web dashboard. Sign in with a Client or Provider account.';

  @override
  String get errorsEmailAlreadyRegistered =>
      'Sign in instead or choose another email address.';

  @override
  String get errorsEmailFieldRequired => 'The Email field is required.';

  @override
  String get errorsEmailNotValid =>
      'The Email field is not a valid e-mail address.';

  @override
  String get errorsInvalidCredentials => 'Invalid email or password.';

  @override
  String get errorsInvalidRoleClientProvider =>
      'Role must be Client or Provider.';

  @override
  String get errorsPasswordFieldRequired => 'The Password field is required.';

  @override
  String get errorsPasswordTooShort =>
      'Password must be at least 6 characters.';

  @override
  String get networkCancelled => 'Request cancelled.';

  @override
  String get networkConflict => 'Conflict';

  @override
  String get networkConnectionFailed => 'Connection failed';

  @override
  String get networkFileTooLarge => 'File too large';

  @override
  String get networkInvalidRequest => 'Invalid request';

  @override
  String get networkNotAllowed => 'Not allowed';

  @override
  String get networkNotFound => 'Not found';

  @override
  String get networkRequestFailed => 'Request failed';

  @override
  String get networkServerError => 'Server error';

  @override
  String get networkSignInRequired => 'Sign-in required';

  @override
  String get networkTimeout => 'The server took too long to respond.';

  @override
  String get networkUnexpected => 'Unexpected network error.';

  @override
  String get networkUnreachable =>
      'Could not reach the server. Check your connection.';
}
