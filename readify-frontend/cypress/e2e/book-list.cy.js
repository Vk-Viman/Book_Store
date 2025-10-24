describe('Book list flow', () => {
  it('loads the book list and navigates to a detail', () => {
    cy.visit('http://localhost:4200/books');
    cy.get('input[placeholder="Search books..."]').should('exist');
    cy.get('.card').first().find('a').contains('View').click();
    cy.url().should('include', '/books/');
    cy.get('h2').should('exist');
  });
});
