import { QueryState } from '@/components/QueryState';
import { useCategoriesQuery } from '../api';

export function CategoriesPage() {
  const query = useCategoriesQuery();

  return (
    <section>
      <h1>Categories</h1>
      <QueryState query={query} isEmpty={(data) => data.items.length === 0}>
        {(data) => (
          <table>
            <thead>
              <tr>
                <th scope="col">Name</th>
                <th scope="col">Description</th>
                <th scope="col">Active</th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((category) => (
                <tr key={category.id}>
                  <td>{category.name}</td>
                  <td>{category.description}</td>
                  <td>{category.isActive ? 'Yes' : 'No'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </QueryState>
    </section>
  );
}
