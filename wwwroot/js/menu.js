$(document).ready(function () {
  const menus = {
    home: {
      left: [
        { label: "Home", link: "/" },
        { label: "Login", link: "/Home/Authentication?action=Login" },
      ],
      right: [
        { label: "Register", link: "/Home/Authentication?action=Register" },
      ],
      profile: [],
    },
    user: {
      left: [
        { label: "Home", link: "/User/Index" },
        { label: "Apply for Services", link: "/User/ServicesList" },
      ],
      right: [
        {
          label: "View Status of Application",
          dropdown: [
            {
              label: "Track Application Status",
              link: "/User/ApplicationStatus",
            },
            {
              label: "View Incomplete Applications",
              link: "/User/IncompleteApplications",
            },
          ],
        },
        { label: "Submit Feedback", link: "/User/Feedback" },
      ],
      profile: [
        {
          label: `<img src="${profile}" class="img-fluid" style="width:40px;border:2px solid black;border-radius:25px"/>`,
          dropdown: [
            { label: "Manage Profile", link: "/Profile/Index" },
            { label: "Settings", link: "/Profile/Settings" },
            { label: "Logout", link: "/Home/Logout" },
          ],
        },
      ],
    },
    officer: {
      left: [{ label: "Home", link: "/Officer/Index" }],
      right: [
        {
          label: "DSC Management",
          dropdown: [
            {
              label: "Register DSC",
              link: "/Officer/RegisterDSC",
            },
            {
              label: "Unregister DSC",
              link: "/Officer/UnregisterDSC",
            },
          ],
        },
        {
          label: "Bank File Management",
          dropdown: [
            {
              label: "Send File To Bank",
              link: "/Officer/SendBankFile",
            },
            {
              label: "Get Bank Response File",
              link: "/Officer/GetResponseFile",
            },
          ],
        },
        { label: "Reports", link: "/Officer/Reports" },
      ],
      profile: [
        {
          label: `<img src="${profile}" class="img-fluid" style="width:40px;border:2px solid black;border-radius:25px"/>`,
          dropdown: [
            { label: "Manage Profile", link: "/Profile/Index" },
            { label: "Settings", link: "/Profile/Settings" },
            { label: "Logout", link: "/Home/Logout" },
          ],
        },
      ],
    },
  };

  let Type =
    userType == "" ? "home" : userType == "Citizen" ? "user" : "officer";

  if (Type == "officer" && userName != "directorFinance") {
    menus.officer.right.splice(1, 1);
  }

  const { left: LeftMenu, right: RightMenu, profile: Profile } = menus[Type];

  const $leftContainer = $("#leftContainer");
  const $rightContainer = $("#rightContainer");
  const $profileContainer = $("#profileContainer");

  function renderMenuItems(menuItems, container) {
    const id = container.attr("id");
    menuItems.forEach((item) => {
      let element;
      if (item.dropdown) {
        const dropdownItems = item.dropdown
          .map(
            (dropItem) =>
              `<li><a class="dropdown-item" href="${dropItem.link}">${dropItem.label}</a></li>`
          )
          .join("");
        element = `
          <li class="nav-item ${
            id == "profileContainer" ? "dropstart " : "dropend "
          }">
            <a class="nav-link dropdown-toggle" href="#" id="navbarDropdownMenuLink" role="button"
                data-bs-toggle="dropdown" aria-expanded="false">
                ${item.label}
            </a>
            <ul class="dropdown-menu p-3 border-0 rounded shadow" aria-labelledby="navbarDropdownMenuLink">
                ${dropdownItems}
            </ul>
          </li>`;
      } else {
        element = `
          <li class="nav-item">
            <a class="nav-link" href="${item.link}">${item.label}</a>
          </li>`;
      }
      container.append(element);
    });
  }

  renderMenuItems(LeftMenu, $leftContainer);
  renderMenuItems(RightMenu, $rightContainer);
  renderMenuItems(Profile, $profileContainer);
});
