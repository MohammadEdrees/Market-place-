import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'catalog_repository.dart';
import 'models.dart';

/// Product detail — invalidated after buy/edit actions to refresh stock.
final productProvider = FutureProvider.autoDispose.family<Product, int>(
  (ref, id) => ref.watch(catalogRepositoryProvider).getProduct(id),
);

/// Service detail — invalidated after reserve/edit actions.
final serviceProvider = FutureProvider.autoDispose.family<Service, int>(
  (ref, id) => ref.watch(catalogRepositoryProvider).getService(id),
);

/// The signed-in provider's own listings (My Listings tab).
final myProductsProvider = FutureProvider.autoDispose<List<Product>>(
  (ref) => ref.watch(catalogRepositoryProvider).myProducts(),
);

final myServicesProvider = FutureProvider.autoDispose<List<Service>>(
  (ref) => ref.watch(catalogRepositoryProvider).myServices(),
);

/// Advertisement slides shown above the category chips on the Products page.
final activeAdvertisementsProvider = FutureProvider<List<Advertisement>>(
  (ref) => ref.watch(catalogRepositoryProvider).activeAdvertisements(),
);
