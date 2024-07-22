async function getDistricts() {
  let options = [];
  try {
    const res = await fetch("/User/GetDistricts", { method: "get" });
    const data = await res.json();
    if (data.status) {
      data.districts.forEach((item) => {
        options.push(
          `<option value="${item.districtId}">${item.districtName}</option>`
        );
      });
    }
  } catch (error) {
    console.error("Error fetching districts:", error);
  }
  return options;
}
async function getTehsils(id) {
  let options = ``;
  try {
    const res = await fetch("/User/GetTehsils?districtId=" + id, {
      method: "get",
    });
    const data = await res.json();
    if (data.status) {
      data.tehsils.forEach((item) => {
        options += `<option value="${item.tehsilId}">${item.tehsilName}</option>`;
      });
    }
  } catch (error) {
    console.error("Error fetching districts:", error);
  }
  return options;
}
async function getBlocks(id) {
  let options = ``;
  try {
    const res = await fetch("/User/GetBlocks?districtId=" + id, {
      method: "get",
    });
    const data = await res.json();
    if (data.status) {
      data.blocks.forEach((item) => {
        options += `<option value="${item.blockId}">${item.blockName}</option>`;
      });
    }
  } catch (error) {
    console.error("Error fetching districts:", error);
  }
  return options;
}
