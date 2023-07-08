export default function getIndexOfFirstCharacter(items, character) {
  return items.findIndex((item) => {
    const firstCharacter = 'sortName' in item ? item.sortName.charAt(0) : item.sortTitle.charAt(0);

    if (character === '#') {
      return !isNaN(Number(firstCharacter));
    }

    return firstCharacter === character;
  });
}
