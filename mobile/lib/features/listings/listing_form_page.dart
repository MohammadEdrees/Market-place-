import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';

import '../../core/widgets/listing_image.dart';
import '../../core/widgets/state_views.dart';
import '../catalog/catalog_providers.dart';
import '../catalog/catalog_repository.dart';
import '../catalog/models.dart';

/// Create or edit a listing (product or service) and manage its gallery.
///
/// URL shapes: `/listings/product/new`, `/listings/service/5`.
class ListingFormPage extends ConsumerStatefulWidget {
  const ListingFormPage({super.key, required this.kind, this.id});

  /// `product` or `service`.
  final String kind;

  /// Existing listing id; `null` for creation.
  final int? id;

  @override
  ConsumerState<ListingFormPage> createState() => _ListingFormPageState();
}

class _ListingFormPageState extends ConsumerState<ListingFormPage> {
  final _formKey = GlobalKey<FormState>();

  // Product fields
  final _name = TextEditingController();
  final _sku = TextEditingController();
  final _price = TextEditingController();
  final _stock = TextEditingController();

  // Service fields
  final _title = TextEditingController();
  final _description = TextEditingController();
  final _cost = TextEditingController();
  final _contactInfo = TextEditingController();
  final _location = TextEditingController();
  final _offers = TextEditingController();

  final _category = TextEditingController();

  List<GalleryImage> _images = const [];
  bool _loading = false;
  bool _saving = false;
  bool _uploading = false;
  String? _error;

  bool get _isProduct => widget.kind == 'product';
  String get _kindPlural => _isProduct ? 'products' : 'services';

  @override
  void initState() {
    super.initState();
    if (widget.id != null) _loadExisting();
  }

  @override
  void dispose() {
    _name.dispose();
    _sku.dispose();
    _price.dispose();
    _stock.dispose();
    _title.dispose();
    _description.dispose();
    _cost.dispose();
    _contactInfo.dispose();
    _location.dispose();
    _offers.dispose();
    _category.dispose();
    super.dispose();
  }

  Future<void> _loadExisting() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      if (_isProduct) {
        final product =
            await ref.read(catalogRepositoryProvider).getProduct(widget.id!);
        _name.text = product.name;
        _sku.text = product.sku;
        _category.text = product.category;
        _price.text = product.price.toString();
        _stock.text = product.stock.toString();
        _images = product.images;
      } else {
        final service =
            await ref.read(catalogRepositoryProvider).getService(widget.id!);
        _title.text = service.title;
        _description.text = service.description;
        _category.text = service.category;
        _cost.text = service.cost.toString();
        _contactInfo.text = service.contactInfo;
        _location.text = service.location;
        _offers.text = service.offers;
        _images = service.images;
      }
      if (mounted) setState(() => _loading = false);
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = errorMessage(e);
          _loading = false;
        });
      }
    }
  }

  double _parseAmount(String raw) {
    final normalized = raw.trim().replaceAll(',', '.');
    return double.tryParse(normalized) ?? -1;
  }

  Future<void> _save() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    setState(() => _saving = true);

    try {
      final repo = ref.read(catalogRepositoryProvider);
      if (_isProduct) {
        final price = _parseAmount(_price.text);
        final stock = int.tryParse(_stock.text.trim()) ?? -1;
        if (widget.id == null) {
          final created = await repo.createProduct(
            name: _name.text.trim(),
            sku: _sku.text.trim(),
            category: _category.text.trim(),
            price: price,
            stock: stock,
          );
          if (!mounted) return;
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Saved — now add some photos.')),
          );
          context.go('/listings/product/${created.id}');
        } else {
          await repo.updateProduct(
            widget.id!,
            name: _name.text.trim(),
            sku: _sku.text.trim(),
            category: _category.text.trim(),
            price: price,
            stock: stock,
          );
          ref.invalidate(myProductsProvider);
          if (mounted) {
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(content: Text('Changes saved.')),
            );
          }
        }
      } else {
        final cost = _parseAmount(_cost.text);
        if (widget.id == null) {
          final created = await repo.createService(
            title: _title.text.trim(),
            description: _description.text.trim(),
            category: _category.text.trim(),
            cost: cost,
            contactInfo: _contactInfo.text.trim(),
            location: _location.text.trim(),
            offers: _offers.text.trim(),
          );
          if (!mounted) return;
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Saved — now add some photos.')),
          );
          context.go('/listings/service/${created.id}');
        } else {
          await repo.updateService(
            widget.id!,
            title: _title.text.trim(),
            description: _description.text.trim(),
            category: _category.text.trim(),
            cost: cost,
            contactInfo: _contactInfo.text.trim(),
            location: _location.text.trim(),
            offers: _offers.text.trim(),
          );
          ref.invalidate(myServicesProvider);
          if (mounted) {
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(content: Text('Changes saved.')),
            );
          }
        }
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(errorMessage(e)),
            backgroundColor: Theme.of(context).colorScheme.error,
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  Future<void> _reloadImages() async {
    final repo = ref.read(catalogRepositoryProvider);
    if (_isProduct) {
      final product = await repo.getProduct(widget.id!);
      _images = product.images;
    } else {
      final service = await repo.getService(widget.id!);
      _images = service.images;
    }
    if (mounted) setState(() {});
  }

  Future<void> _addImage() async {
    final picked = await ImagePicker().pickImage(
      source: ImageSource.gallery,
      maxWidth: 1800,
      imageQuality: 85,
    );
    if (picked == null || !mounted) return;

    setState(() => _uploading = true);
    try {
      await ref.read(catalogRepositoryProvider).addListingImage(
            kind: _kindPlural,
            id: widget.id!,
            filePath: picked.path,
            fileName: picked.name,
          );
      await _reloadImages();
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(const SnackBar(content: Text('Photo added.')));
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(errorMessage(e)),
            backgroundColor: Theme.of(context).colorScheme.error,
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _uploading = false);
    }
  }

  Future<void> _deleteImage(GalleryImage image) async {
    try {
      await ref.read(catalogRepositoryProvider).deleteListingImage(
            kind: _kindPlural,
            id: widget.id!,
            imageId: image.id,
          );
      setState(() => _images = [
            for (final existing in _images)
              if (existing.id != image.id) existing,
          ]);
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(const SnackBar(content: Text('Photo removed.')));
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(errorMessage(e)),
            backgroundColor: Theme.of(context).colorScheme.error,
          ),
        );
      }
    }
  }

  String? _required(String? value, [int? max]) {
    final v = value?.trim() ?? '';
    if (v.isEmpty) return 'Required.';
    if (max != null && v.length > max) return 'Max $max characters.';
    return null;
  }

  String? _optionalMax(String? value, int max) {
    final v = value?.trim() ?? '';
    if (v.length > max) return 'Max $max characters.';
    return null;
  }

  String? _amountValidator(String? value) {
    final v = value?.trim() ?? '';
    if (v.isEmpty) return 'Required.';
    if (_parseAmount(v) < 0) return 'Enter a valid amount.';
    return null;
  }

  String? _intValidator(String? value) {
    final v = value?.trim() ?? '';
    if (v.isEmpty) return 'Required.';
    final parsed = int.tryParse(v);
    if (parsed == null || parsed < 0) return 'Enter 0 or more.';
    return null;
  }

  @override
  Widget build(BuildContext context) {
    final noun = _isProduct ? 'product' : 'service';
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.id == null ? 'New $noun' : 'Edit $noun'),
      ),
      body: _buildBody(noun),
      bottomNavigationBar: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
          child: FilledButton(
            onPressed: (_loading || _saving) ? null : _save,
            style: FilledButton.styleFrom(padding: const EdgeInsets.symmetric(vertical: 16)),
            child: _saving
                ? const SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : Text(widget.id == null ? 'Create $noun' : 'Save changes'),
          ),
        ),
      ),
    );
  }

  Widget _buildBody(String noun) {
    if (_loading) return const LoadingView();
    if (_error != null && widget.id != null && _images.isEmpty && _name.text.isEmpty && _title.text.isEmpty) {
      return ErrorView(message: _error!, onRetry: _loadExisting);
    }

    return Form(
      key: _formKey,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          if (_isProduct) ...[
            TextFormField(
              controller: _name,
              textInputAction: TextInputAction.next,
              maxLength: 120,
              decoration: const InputDecoration(
                labelText: 'Name',
                border: OutlineInputBorder(),
                counterText: '',
              ),
              validator: (v) => _required(v, 120),
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                Expanded(
                  child: TextFormField(
                    controller: _sku,
                    textInputAction: TextInputAction.next,
                    maxLength: 40,
                    decoration: const InputDecoration(
                      labelText: 'SKU',
                      border: OutlineInputBorder(),
                      counterText: '',
                    ),
                    validator: (v) => _required(v, 40),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: TextFormField(
                    controller: _category,
                    textInputAction: TextInputAction.next,
                    maxLength: 60,
                    decoration: const InputDecoration(
                      labelText: 'Category',
                      hintText: 'e.g. Electronics',
                      border: OutlineInputBorder(),
                      counterText: '',
                    ),
                    validator: (v) => _required(v, 60),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                Expanded(
                  child: TextFormField(
                    controller: _price,
                    keyboardType: const TextInputType.numberWithOptions(
                      decimal: true,
                    ),
                    decoration: const InputDecoration(
                      labelText: 'Price (USD)',
                      prefixText: r'$ ',
                      border: OutlineInputBorder(),
                    ),
                    validator: _amountValidator,
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: TextFormField(
                    controller: _stock,
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(
                      labelText: 'Stock',
                      border: OutlineInputBorder(),
                    ),
                    validator: _intValidator,
                  ),
                ),
              ],
            ),
          ] else ...[
            TextFormField(
              controller: _title,
              textInputAction: TextInputAction.next,
              maxLength: 120,
              decoration: const InputDecoration(
                labelText: 'Title',
                border: OutlineInputBorder(),
                counterText: '',
              ),
              validator: (v) => _required(v, 120),
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _description,
              textInputAction: TextInputAction.next,
              maxLength: 1000,
              maxLines: 4,
              decoration: const InputDecoration(
                labelText: 'Description',
                hintText: 'What do you offer?',
                alignLabelWithHint: true,
                border: OutlineInputBorder(),
                counterText: '',
              ),
              validator: (v) => _optionalMax(v, 1000),
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                Expanded(
                  child: TextFormField(
                    controller: _category,
                    textInputAction: TextInputAction.next,
                    maxLength: 60,
                    decoration: const InputDecoration(
                      labelText: 'Category',
                      hintText: 'e.g. Repairs',
                      border: OutlineInputBorder(),
                      counterText: '',
                    ),
                    validator: (v) => _required(v, 60),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: TextFormField(
                    controller: _cost,
                    keyboardType: const TextInputType.numberWithOptions(
                      decimal: true,
                    ),
                    decoration: const InputDecoration(
                      labelText: 'Cost (USD)',
                      prefixText: r'$ ',
                      border: OutlineInputBorder(),
                    ),
                    validator: _amountValidator,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _contactInfo,
              textInputAction: TextInputAction.next,
              maxLength: 200,
              decoration: const InputDecoration(
                labelText: 'Contact info',
                hintText: 'Phone, email or WhatsApp',
                border: OutlineInputBorder(),
                counterText: '',
              ),
              validator: (v) => _required(v, 200),
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _location,
              textInputAction: TextInputAction.next,
              maxLength: 120,
              decoration: const InputDecoration(
                labelText: 'Location / service area',
                border: OutlineInputBorder(),
                counterText: '',
              ),
              validator: (v) => _required(v, 120),
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _offers,
              textInputAction: TextInputAction.done,
              maxLength: 500,
              maxLines: 2,
              decoration: const InputDecoration(
                labelText: 'Current offers (optional)',
                hintText: 'e.g. 20% off the first booking',
                alignLabelWithHint: true,
                border: OutlineInputBorder(),
                counterText: '',
              ),
              validator: (v) => _optionalMax(v, 500),
            ),
          ],
          const SizedBox(height: 24),
          Row(
            children: [
              Text(
                'Photos',
                style: Theme.of(context)
                    .textTheme
                    .titleMedium
                    ?.copyWith(fontWeight: FontWeight.w700),
              ),
              const SizedBox(width: 8),
              Text(
                '${_images.length}',
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                      color: Theme.of(context).colorScheme.onSurfaceVariant,
                    ),
              ),
            ],
          ),
          const SizedBox(height: 4),
          Text(
            widget.id == null
                ? 'Save the $noun first, then add photos here.'
                : 'First photo is shown as the thumbnail.',
            style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                ),
          ),
          const SizedBox(height: 12),
          GridView.builder(
            shrinkWrap: true,
            physics: const NeverScrollableScrollPhysics(),
            gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
              crossAxisCount: 4,
              mainAxisSpacing: 8,
              crossAxisSpacing: 8,
            ),
            itemCount: _images.length + 1,
            itemBuilder: (context, index) {
              if (index == _images.length) {
                return InkWell(
                  onTap: widget.id == null || _uploading ? null : _addImage,
                  borderRadius: BorderRadius.circular(10),
                  child: Container(
                    decoration: BoxDecoration(
                      color: Theme.of(context)
                          .colorScheme
                          .surfaceContainerHighest,
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: _uploading
                        ? const Center(
                            child: SizedBox(
                              width: 20,
                              height: 20,
                              child:
                                  CircularProgressIndicator(strokeWidth: 2),
                            ),
                          )
                        : const Icon(Icons.add_a_photo_outlined),
                  ),
                );
              }
              final image = _images[index];
              return Stack(
                fit: StackFit.expand,
                children: [
                  ClipRRect(
                    borderRadius: BorderRadius.circular(10),
                    child: ListingImage(url: image.path),
                  ),
                  Positioned(
                    top: 4,
                    right: 4,
                    child: Material(
                      color: Colors.black54,
                      shape: const CircleBorder(),
                      child: InkWell(
                        customBorder: const CircleBorder(),
                        onTap: () => _deleteImage(image),
                        child: const Padding(
                          padding: EdgeInsets.all(4),
                          child: Icon(
                            Icons.close,
                            size: 16,
                            color: Colors.white,
                          ),
                        ),
                      ),
                    ),
                  ),
                ],
              );
            },
          ),
          const SizedBox(height: 24),
        ],
      ),
    );
  }
}
