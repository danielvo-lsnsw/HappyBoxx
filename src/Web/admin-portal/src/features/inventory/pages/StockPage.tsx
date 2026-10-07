import { QueryState } from '@/components/QueryState';
import { useStockQuery } from '../api';

export function StockPage() {
  const query = useStockQuery();

  return (
    <section>
      <h1>Stock</h1>
      <QueryState query={query} isEmpty={(data) => data.items.length === 0}>
        {(data) => (
          <table>
            <thead>
              <tr>
                <th scope="col">SKU</th>
                <th scope="col">On hand</th>
                <th scope="col">Reorder level</th>
                <th scope="col">Status</th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((item) => (
                <tr key={item.id}>
                  <td>{item.sku}</td>
                  <td>{item.quantityOnHand}</td>
                  <td>{item.reorderLevel}</td>
                  <td>{item.isLowStock ? 'Low stock' : 'OK'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </QueryState>
    </section>
  );
}
