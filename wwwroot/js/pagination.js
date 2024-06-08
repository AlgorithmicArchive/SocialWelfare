$(document).ready(function () {
  var pageSize = 5;
  var pageCount = $(".table tbody tr").length / pageSize;
  var currentPage = 1;

  $(".table tbody tr").hide();
  $(".table tbody tr").slice(0, pageSize).show();

  // Only append buttons if there are more than 5 entries
  if (pageCount > 1) {
    const buttonContainer = $(`<div class="center-align gap-2"></div>`);
    for (var i = 0; i < pageCount; i++) {
      buttonContainer.append(
        '<button class="btn btn-dark page-btn">' + (i + 1) + "</button>"
      );
    }
    $(".full-screen-section .container").append(buttonContainer);
  }

  $(".page-btn").click(function () {
    var n = $(this).text();
    currentPage = n;
    $(".table tbody tr").hide();
    var grandTotal = pageSize * currentPage;

    for (var i = grandTotal - pageSize; i < grandTotal; i++) {
      $(".table tbody tr:eq(" + i + ")").show();
    }
  });

  // Function to compare table cell values
  function comparer(index) {
    return function (a, b) {
      var valA = getCellValue(a, index),
        valB = getCellValue(b, index);
      return $.isNumeric(valA) && $.isNumeric(valB)
        ? valA - valB
        : valA.localeCompare(valB);
    };
  }

  // Function to get cell value
  function getCellValue(row, index) {
    return $(row).children("td").eq(index).text();
  }

  // Click event for headers
  $("th").click(function () {
    var table = $(this).parents("table").eq(0);
    var rows = table
      .find("tr:gt(0)")
      .toArray()
      .sort(comparer($(this).index()));
    this.asc = !this.asc; // Toggle the sort direction
    if (!this.asc) {
      rows = rows.reverse();
    }
    for (var i = 0; i < rows.length; i++) {
      table.append(rows[i]);
    }
    // Update the display of the current page
    $(".table tbody tr").hide();
    var grandTotal = pageSize * currentPage;
    for (var i = grandTotal - pageSize; i < grandTotal; i++) {
      $(".table tbody tr:eq(" + i + ")").show();
    }
  });
});
