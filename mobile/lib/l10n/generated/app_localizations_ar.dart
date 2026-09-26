// ignore: unused_import
import 'package:intl/intl.dart' as intl;

import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Arabic (`ar`).
class AppLocalizationsAr extends AppLocalizations {
  AppLocalizationsAr([String locale = 'ar']) : super(locale);

  @override
  String get commonAll => 'الكل';

  @override
  String get commonCancel => 'إلغاء';

  @override
  String get commonDelete => 'حذف';

  @override
  String get commonEdit => 'تعديل';

  @override
  String get commonEmailInvalid => 'أدخل بريدًا إلكترونيًا صالحًا.';

  @override
  String get commonEmailLabel => 'البريد الإلكتروني';

  @override
  String get commonEmailRequired => 'البريد الإلكتروني مطلوب.';

  @override
  String get commonGenericError => 'حدث خطأ ما. يرجى المحاولة مرة أخرى.';

  @override
  String get commonLanguage => 'اللغة';

  @override
  String get commonLanguageArabic => 'العربية';

  @override
  String get commonLanguageEnglish => 'English';

  @override
  String commonMaxCharacters(String count) {
    return 'الحد الأقصى $count حرفًا.';
  }

  @override
  String get commonMinCharacters => '6 أحرف على الأقل.';

  @override
  String get commonNameRequired => 'الاسم مطلوب.';

  @override
  String get commonPasswordLabel => 'كلمة المرور';

  @override
  String get commonPasswordRequired => 'كلمة المرور مطلوبة.';

  @override
  String get commonPasswordsDoNotMatch => 'كلمتا المرور غير متطابقتين.';

  @override
  String get commonRequired => 'مطلوب.';

  @override
  String get commonSaveChanges => 'حفظ التغييرات';

  @override
  String get commonSomethingWentWrong => 'حدث خطأ ما.';

  @override
  String get commonTryAgain => 'إعادة المحاولة';

  @override
  String get navBrowse => 'تصفح';

  @override
  String get navListings => 'العروض';

  @override
  String get navOrders => 'الطلبات';

  @override
  String get navProfile => 'الملف الشخصي';

  @override
  String get navServices => 'الخدمات';

  @override
  String get loginCreateAccount => 'جديد هنا؟ أنشئ حسابًا';

  @override
  String get loginSignIn => 'تسجيل الدخول';

  @override
  String get loginTagline => 'اشترِ المنتجات واحجز الخدمات، أو بِع ما تملك.';

  @override
  String get registerClientHint =>
      'تصفّح الكتالوج، واشترِ المنتجات واحجز الخدمات.';

  @override
  String get registerConfirmPassword => 'تأكيد كلمة المرور';

  @override
  String get registerCreateAccount => 'إنشاء حساب';

  @override
  String get registerFullName => 'الاسم الكامل';

  @override
  String get registerHaveAccount => 'لدي حساب بالفعل';

  @override
  String get registerHeading => 'انضم إلى Market Workplace';

  @override
  String get registerLocation => 'الموقع';

  @override
  String get registerOptionalContact => 'بيانات تواصل اختيارية';

  @override
  String get registerPhone => 'الهاتف';

  @override
  String get registerProviderHint => 'بِع منتجات وقدّم خدماتك الخاصة.';

  @override
  String get registerSubtitle => 'اختر كيف تريد استخدام السوق.';

  @override
  String get registerTitle => 'إنشاء حساب';

  @override
  String get productsEmpty => 'لا توجد منتجات مطابقة لبحثك.';

  @override
  String get productsLoadError => 'تعذّر تحميل المنتجات.';

  @override
  String get productsSearchHint => 'ابحث عن المنتجات';

  @override
  String get productsTitle => 'تصفح';

  @override
  String get servicesEmpty => 'لا توجد خدمات مطابقة لبحثك.';

  @override
  String get servicesLoadError => 'تعذّر تحميل الخدمات.';

  @override
  String get servicesSearchHint => 'ابحث عن الخدمات';

  @override
  String get servicesTitle => 'الخدمات';

  @override
  String get productDetailBuy => 'شراء';

  @override
  String get productDetailBuyNow => 'اشترِ الآن';

  @override
  String productDetailConfirmMessage(String name, String price) {
    return 'هل ترغب بشراء «$name» مقابل $price؟';
  }

  @override
  String get productDetailConfirmTitle => 'تأكيد الشراء';

  @override
  String get productDetailEditListing => 'تعديل الإعلان';

  @override
  String productDetailInStock(String count) {
    return '$count متوفر';
  }

  @override
  String get productDetailNoneLeft => 'لا يوجد متبقٍ';

  @override
  String get productDetailOrderPlaced =>
      'تم إتمام الطلب — تابِعه من قسم الطلبات.';

  @override
  String productDetailSold(String count) {
    return 'تم بيع $count';
  }

  @override
  String get productDetailTitle => 'المنتج';

  @override
  String get productDetailYours => 'هذا إعلانك.';

  @override
  String get serviceDetailAbout => 'عن هذه الخدمة';

  @override
  String serviceDetailConfirmMessage(String title, String price) {
    return 'هل ترغب بحجز «$title» مقابل $price؟';
  }

  @override
  String get serviceDetailConfirmTitle => 'تأكيد الحجز';

  @override
  String get serviceDetailContact => 'التواصل';

  @override
  String get serviceDetailEditService => 'تعديل الخدمة';

  @override
  String get serviceDetailReserve => 'احجز';

  @override
  String get serviceDetailReserved => 'تم الحجز — تابِعه من قسم الطلبات.';

  @override
  String get serviceDetailTitle => 'الخدمة';

  @override
  String get serviceDetailUnavailable => 'غير متاحة حاليًا';

  @override
  String get serviceDetailYours => 'هذه خدمتك.';

  @override
  String get ordersAnyStatus => 'أي حالة';

  @override
  String get ordersCategory => 'الفئة';

  @override
  String get ordersCustomer => 'العميل';

  @override
  String get ordersDate => 'التاريخ';

  @override
  String get ordersEmpty => 'لا توجد طلبات بعد.';

  @override
  String get ordersFilterByStatus => 'تصفية حسب الحالة';

  @override
  String ordersNumber(String id) {
    return 'الطلب #$id';
  }

  @override
  String get ordersProductPurchase => 'شراء منتج';

  @override
  String get ordersProductsFilter => 'المنتجات';

  @override
  String get ordersSearchHint => 'ابحث في الطلبات';

  @override
  String get ordersServiceReservation => 'حجز خدمة';

  @override
  String get ordersServicesFilter => 'الخدمات';

  @override
  String get ordersStatus => 'الحالة';

  @override
  String ordersStatusUpdated(String id, String status) {
    return 'تم تحديث الطلب #$id إلى $status';
  }

  @override
  String get ordersTitle => 'الطلبات';

  @override
  String get ordersTotal => 'الإجمالي';

  @override
  String get ordersUpdateStatus => 'تحديث الحالة';

  @override
  String get myListingsCreate => 'إنشاء';

  @override
  String myListingsDeleteProductMessage(String name) {
    return 'سيُزال «$name» نهائيًا.';
  }

  @override
  String get myListingsDeleteProductTitle => 'حذف المنتج؟';

  @override
  String myListingsDeleteServiceMessage(String title) {
    return 'سيُزال «$title» نهائيًا.';
  }

  @override
  String get myListingsDeleteServiceTitle => 'حذف الخدمة؟';

  @override
  String myListingsDeletedProduct(String name) {
    return 'تم حذف «$name».';
  }

  @override
  String myListingsDeletedService(String title) {
    return 'تم حذف «$title».';
  }

  @override
  String get myListingsEmptyProducts => 'لا تملك منتجات بعد. أنشئ أول منتج لك.';

  @override
  String get myListingsEmptyServices => 'لا تملك خدمات بعد. أنشئ أول خدمة لك.';

  @override
  String get myListingsNewProduct => 'منتج جديد';

  @override
  String get myListingsNewService => 'خدمة جديدة';

  @override
  String get myListingsProductsTab => 'المنتجات';

  @override
  String get myListingsServicesTab => 'الخدمات';

  @override
  String myListingsStockAndSold(String sold, String stock) {
    return '$stock متوفر · تم بيع $sold';
  }

  @override
  String get myListingsTitle => 'إعلاناتي';

  @override
  String get listingFormCategory => 'الفئة';

  @override
  String get listingFormCategoryHintProduct => 'مثال: الإلكترونيات';

  @override
  String get listingFormCategoryHintService => 'مثال: الإصلاحات';

  @override
  String get listingFormChangesSaved => 'تم حفظ التغييرات.';

  @override
  String get listingFormContactHint => 'الهاتف أو البريد أو واتساب';

  @override
  String get listingFormContactInfo => 'بيانات التواصل';

  @override
  String get listingFormCost => 'التكلفة (USD)';

  @override
  String get listingFormCreateProduct => 'إنشاء منتج';

  @override
  String get listingFormCreateService => 'إنشاء خدمة';

  @override
  String get listingFormDescription => 'الوصف';

  @override
  String get listingFormDescriptionHint => 'ماذا تقدّم؟';

  @override
  String get listingFormEditProduct => 'تعديل المنتج';

  @override
  String get listingFormEditService => 'تعديل الخدمة';

  @override
  String get listingFormLocation => 'الموقع / منطقة الخدمة';

  @override
  String get listingFormName => 'الاسم';

  @override
  String get listingFormNewProduct => 'منتج جديد';

  @override
  String get listingFormNewService => 'خدمة جديدة';

  @override
  String get listingFormNonNegative => 'أدخل 0 أو أكثر.';

  @override
  String get listingFormOffers => 'العروض الحالية (اختياري)';

  @override
  String get listingFormOffersHint => 'مثال: خصم 20% على أول حجز';

  @override
  String get listingFormPhotoAdded => 'تمت إضافة الصورة.';

  @override
  String get listingFormPhotoRemoved => 'تمت إزالة الصورة.';

  @override
  String get listingFormPhotos => 'الصور';

  @override
  String get listingFormPrice => 'السعر (USD)';

  @override
  String get listingFormSaveFirstProduct =>
      'احفظ المنتج أولًا، ثم أضف الصور هنا.';

  @override
  String get listingFormSaveFirstService =>
      'احفظ الخدمة أولًا، ثم أضف الصور هنا.';

  @override
  String get listingFormSavedAddPhotos => 'تم الحفظ — أضف بعض الصور الآن.';

  @override
  String get listingFormSku => 'رمز المنتج';

  @override
  String get listingFormStock => 'المخزون';

  @override
  String get listingFormThumbnailHint => 'تُعرض أول صورة كصورة مصغّرة.';

  @override
  String get listingFormTitle => 'العنوان';

  @override
  String get listingFormValidAmount => 'أدخل مبلغًا صالحًا.';

  @override
  String get profileBio => 'نبذة';

  @override
  String get profileChoosePhoto => 'اختيار صورة';

  @override
  String get profileClearToRemove => 'اتركه فارغًا للإزالة.';

  @override
  String get profileEditProfile => 'تعديل الملف الشخصي';

  @override
  String get profileFullName => 'الاسم الكامل';

  @override
  String get profileLocation => 'الموقع';

  @override
  String get profileNoPlan => 'لم تُسند إليك أي باقة بعد.';

  @override
  String get profilePhone => 'الهاتف';

  @override
  String get profilePhotoRemoved => 'تمت إزالة الصورة.';

  @override
  String get profilePhotoUpdated => 'تم تحديث الصورة.';

  @override
  String get profileRemovePhoto => 'إزالة الصورة';

  @override
  String get profileSignOut => 'تسجيل الخروج';

  @override
  String get profileSignOutMessage =>
      'ستحتاج إلى تسجيل الدخول مرة أخرى للمتابعة.';

  @override
  String get profileSignOutTitle => 'تسجيل الخروج؟';

  @override
  String get profileSubscription => 'الاشتراك';

  @override
  String get profileTitle => 'الملف الشخصي';

  @override
  String get profileUpdated => 'تم تحديث الملف الشخصي.';

  @override
  String get statusActive => 'نشط';

  @override
  String get statusCancelled => 'ملغي';

  @override
  String get statusClient => 'عميل';

  @override
  String get statusCompleted => 'مكتمل';

  @override
  String get statusConfirmed => 'مؤكد';

  @override
  String get statusDashboard => 'لوحة التحكم';

  @override
  String get statusExpired => 'منتهٍ';

  @override
  String get statusInactive => 'غير نشط';

  @override
  String get statusLowStock => 'مخزون منخفض';

  @override
  String get statusMobile => 'جوال';

  @override
  String get statusOutOfStock => 'نفد المخزون';

  @override
  String get statusProcessing => 'قيد المعالجة';

  @override
  String get statusProvider => 'مزوّد';

  @override
  String get statusRefunded => 'مسترد';

  @override
  String get statusReserved => 'محجوز';

  @override
  String get planBasic => 'أساسي';

  @override
  String get planEnterprise => 'مؤسسي';

  @override
  String get planPremium => 'بريميوم';

  @override
  String get subscriptionAutoRenewOff => 'لن يُجدَّد تلقائيًا';

  @override
  String get subscriptionAutoRenewOn => 'يُجدَّد تلقائيًا';

  @override
  String get subscriptionOpenEnded => 'بلا نهاية';

  @override
  String get subscriptionPerMonth => 'شهريًا';

  @override
  String get subscriptionPerYear => 'سنويًا';

  @override
  String get errorsAccountExists => 'يوجد حساب بهذا البريد الإلكتروني بالفعل.';

  @override
  String get errorsCheckCredentials => 'تحقق من بيانات الدخول وحاول مرة أخرى.';

  @override
  String get errorsDashboardAccount =>
      'هذا الحساب تابع للوحة تحكم الويب. سجّل الدخول بحساب عميل أو مزوّد.';

  @override
  String get errorsEmailAlreadyRegistered =>
      'سجّل الدخول أو اختر بريدًا إلكترونيًا آخر.';

  @override
  String get errorsEmailFieldRequired => 'حقل البريد الإلكتروني مطلوب.';

  @override
  String get errorsEmailNotValid => 'حقل البريد الإلكتروني ليس عنوانًا صالحًا.';

  @override
  String get errorsInvalidCredentials =>
      'البريد الإلكتروني أو كلمة المرور غير صحيحة.';

  @override
  String get errorsInvalidRoleClientProvider =>
      'يجب أن يكون الدور عميلًا أو مزوّدًا.';

  @override
  String get errorsPasswordFieldRequired => 'حقل كلمة المرور مطلوب.';

  @override
  String get errorsPasswordTooShort =>
      'يجب أن تتكون كلمة المرور من 6 أحرف على الأقل.';

  @override
  String get networkCancelled => 'أُلغيت الطلب.';

  @override
  String get networkConflict => 'تعارض';

  @override
  String get networkConnectionFailed => 'فشل الاتصال';

  @override
  String get networkFileTooLarge => 'الملف كبير جدًا';

  @override
  String get networkInvalidRequest => 'طلب غير صالح';

  @override
  String get networkNotAllowed => 'غير مسموح';

  @override
  String get networkNotFound => 'غير موجود';

  @override
  String get networkRequestFailed => 'فشل الطلب';

  @override
  String get networkServerError => 'خطأ في الخادم';

  @override
  String get networkSignInRequired => 'مطلوب تسجيل الدخول';

  @override
  String get networkTimeout => 'استغرق الخادم وقتًا طويلاً للاستجابة.';

  @override
  String get networkUnexpected => 'حدث خطأ شبكة غير متوقع.';

  @override
  String get networkUnreachable =>
      'تعذّر الوصول إلى الخادم. تحقّق من اتصالك بالإنترنت.';
}
