import { QueryState } from '@/components/QueryState';
import { formatCurrency } from '@/lib/format';
import { useProductsQuery } from '../api';

export function ProductsPage() {
  const query = useProductsQuery();

  return (
    <section>
      <h1>Products</h1>
      <QueryState query={query} isEmpty={(data) => data.items.length === 0}>
        {(data) => (
          <table>
            <thead>
              <tr>
                <th scope="col">SKU</th>
                <th scope="col">Name</th>
                <th scope="col">Category</th>
                <th scope="col">Unit</th>
                <th scope="col">Price</th>
                <th scope="col">Active</th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((product) => (
                <tr key={product.id}>
                  <td>{product.sku}</td>
                  <td>{product.name}</td>
                  <td>{product.categoryName}</td>
                  <td>{product.unit}</td>
                  <td>{formatCurrency(product.price)}</td>
                  <td>{product.isActive ? 'Yes' : 'No'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </QueryState>
    </section>
  );
}
